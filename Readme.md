# DTOGenerator

This is a code generator for C# Entity to DTO

## Usage
- source file DemoClass.cs
```C#
using codeGenerate;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace DemoClass
{
    /// <summary>
    /// 人员
    /// </summary>
    public class Person : Exception
    {
        /// <summary>
        /// 名称
        /// </summary>
        [In("PersonInDTO")]
        [Out("PersonListDTO")]
        [MaxLength(60)]
        public string Name { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [In("PersonUpdateDTO")]
        [Out("PersonListDTO|int")]
        public int? Age { get; set; }
    }
}
```
- Generate DTO
```
CSharpSyntaxTree cst = CSharpSyntaxTree.ParseText(File.ReadAllText("DemoClass.cs"), CSharpParseOptions.Default) as CSharpSyntaxTree;
var root = cst.GetRoot();

var ge = new DTOGenerator(cst);
var list = ge.Generate("../codeGenerate/DTO/");
```
- LeftAsIs
> LeftAsIsAttribute can keep properties as it is in DTO.