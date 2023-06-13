// See https://aka.ms/new-console-template for more information

using codeGenerate;
using Microsoft.CodeAnalysis.CSharp;

CSharpSyntaxTree cst = CSharpSyntaxTree.ParseText(File.ReadAllText("DemoClass.cs"), CSharpParseOptions.Default) as CSharpSyntaxTree;
var root = cst.GetRoot();

var ge = new DTOGenerator(cst);
var list = ge.Generate("../codeGenerate/DTO/");

Console.WriteLine("Hello, World!");