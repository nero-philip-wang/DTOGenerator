using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace codeGenerate
{
    public class DTOGenerator
    {
        private SyntaxGenerator generator = SyntaxGenerator.GetGenerator(new AdhocWorkspace(), LanguageNames.CSharp);
        private CSharpSyntaxNode sourceRoot;

        public DTOGenerator(CSharpSyntaxTree sourceTree)
        {
            this.sourceRoot = sourceTree.GetRoot();
        }

        public List<SyntaxNode> Generate(string dirPath)
        {
            IEnumerable<UsingDirectiveSyntax> usingDirctives;
            string namespaceName;
            SyntaxTriviaList classRemark;
            IEnumerable<PropertyDeclarationSyntax> propertyDeclarations;
            Deconstruct(out usingDirctives, out namespaceName, out classRemark, out propertyDeclarations);
            var propSet = Rearrange(propertyDeclarations);

            var resultClass = new List<SyntaxNode>(propSet.Count);
            foreach (var item in propSet)
            {
                Directory.CreateDirectory(dirPath);
                var filepath = Path.Combine(dirPath, $"{item.Key}.cs");
                var leaveAsIs = ReadExsitedProperty(filepath);
                if (leaveAsIs != null)
                {
                    var sameName = item.Value
                        .Where(c => leaveAsIs.Select(d => d.Identifier.ValueText).Contains(c.Identifier.ValueText))
                        .ToArray();
                    foreach (var c in sameName)
                        item.Value.Remove(c);
                    item.Value.AddRange(leaveAsIs);
                }
                var newClassDefinition = generator.ClassDeclaration(
                  item.Key,
                  typeParameters: null,
                  accessibility: Accessibility.Public,
                  modifiers: DeclarationModifiers.Partial,
                  baseType: null,
                  members: item.Value).WithLeadingTrivia(classRemark);
                var namespaceDeclaration = generator.NamespaceDeclaration(namespaceName, newClassDefinition);
                var allNode = usingDirctives.ToList<SyntaxNode>();
                allNode.Add(namespaceDeclaration);
                var newNode = generator.CompilationUnit(allNode).NormalizeWhitespace();
                resultClass.Add(newNode);

                File.WriteAllText(filepath, newNode.ToFullString());
            }
            return resultClass;
        }

        private void Deconstruct(out IEnumerable<UsingDirectiveSyntax> usingDirctives, out string namespaceName, out SyntaxTriviaList classRemark,
            out IEnumerable<PropertyDeclarationSyntax> propertyDeclarations)
        {
            var nodes = sourceRoot.ChildNodes();
            usingDirctives = nodes.OfType<UsingDirectiveSyntax>();
            var ns = nodes.OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            namespaceName = ns.Name.ToString();
            var classDefinition = ns.ChildNodes().Last() as ClassDeclarationSyntax;
            classRemark = classDefinition.HasLeadingTrivia ? classDefinition.GetLeadingTrivia() : default;
            propertyDeclarations = classDefinition.ChildNodes().OfType<PropertyDeclarationSyntax>();
        }

        private Dictionary<string, List<PropertyDeclarationSyntax>> Rearrange(IEnumerable<PropertyDeclarationSyntax> propertyDeclarations)
        {
            var propSet = new Dictionary<string, List<PropertyDeclarationSyntax>>();
            foreach (var property in propertyDeclarations)
            {
                foreach (var attributeList in property.AttributeLists)
                {
                    if (attributeList.GetName() == "In")
                    {
                        var DTOArguments = attributeList.DescendantNodes().OfType<AttributeArgumentListSyntax>().FirstOrDefault()?.Arguments;
                        foreach (var argument in DTOArguments)
                        {
                            var text = argument.GetFirstToken().ValueText.Split("|");
                            var classname = text.First();
                            var renameProp = text.Length > 1 ? text.Last() : default;
                            if (!propSet.ContainsKey(classname))
                                propSet.Add(classname, new List<PropertyDeclarationSyntax>());
                            propSet[classname].Add(Rebuild(property, false, renameProp));
                        }
                    }
                    else if (attributeList.GetName() == "Out")
                    {
                        var DTOArguments = attributeList.DescendantNodes().OfType<AttributeArgumentListSyntax>().FirstOrDefault()?.Arguments;
                        foreach (var argument in DTOArguments)
                        {
                            var text = argument.GetFirstToken().ValueText.Split("|");
                            var classname = text.First();
                            var renameProp = text.Length > 1 ? text.Last() : default;
                            if (!propSet.ContainsKey(classname))
                                propSet.Add(classname, new List<PropertyDeclarationSyntax>());
                            propSet[classname].Add(Rebuild(property, true, renameProp));
                        }
                    }
                }
            }
            return propSet;
        }

        private PropertyDeclarationSyntax Rebuild(PropertyDeclarationSyntax origin, bool ignoreAttritube = false, string? reType = null)
        {
            string[] MARK_ATTRIBUTES = new string[] { "In", "Out", "InAttribute", "OutAttribute", };
            var nodes = origin.ChildNodes();
            var property =
                ((PropertyDeclarationSyntax)generator.PropertyDeclaration(origin.Identifier.ValueText,
                reType == null ? nodes.OfType<TypeSyntax>().First() : SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(reType)),
                Accessibility.Public))
                .AddAttributeLists(
                    ignoreAttritube ?
                    new AttributeListSyntax[0] :
                    nodes.OfType<AttributeListSyntax>().Where(c => !MARK_ATTRIBUTES.Contains(c.GetName())).ToArray());
            if (origin.HasLeadingTrivia) property = property.WithLeadingTrivia(origin.GetLeadingTrivia());
            return property;
        }

        public IEnumerable<PropertyDeclarationSyntax> ReadExsitedProperty(string filename)
        {
            if (!File.Exists(filename)) return null;
            var cst = CSharpSyntaxTree.ParseText(File.ReadAllText(filename), CSharpParseOptions.Default) as CSharpSyntaxTree;
            var sourceRoot = cst.GetRoot();
            var props = sourceRoot.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(c => c.AttributeLists.Any(d => d.GetName() == "LeftAsIs"));
            return props;
        }
    }
}

public static class HelpExt
{
    public static string GetName(this AttributeListSyntax attribute)
    {
        return attribute.GetFirstToken().GetNextToken().ValueText;
    }

    public static string GetName(this PropertyDeclarationSyntax property)
    {
        return property.Identifier.ValueText;
    }
}