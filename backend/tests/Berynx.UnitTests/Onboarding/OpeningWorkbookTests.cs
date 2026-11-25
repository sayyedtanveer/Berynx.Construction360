using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using Berynx.Api.Onboarding;
using NUnit.Framework;
namespace Berynx.UnitTests.Onboarding;

public sealed class OpeningWorkbookTests
{
    [Test] public async Task WorkbookPreviewPreservesExactDecimalAndFlagsInvalidBill()
    {
        XNamespace x="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement Row(int n,params string[] cells)=>new(x+"row",new XAttribute("r",n),
            cells.Select((v,i)=>new XElement(x+"c",new XAttribute("r",$"{(char)('A'+i)}{n}"),new XAttribute("t","inlineStr"),
                new XElement(x+"is",new XElement(x+"t",v)))));
        using var stream=new MemoryStream();
        using(var archive=new ZipArchive(stream,ZipArchiveMode.Create,true))
        using(var output=archive.CreateEntry("xl/worksheets/sheet1.xml").Open()){
            new XDocument(new XElement(x+"worksheet",new XElement(x+"sheetData",
                Row(1,"Kind","Ledger","Bill","Debit","Credit"),
                Row(2,"Bank","Current","","123.45","0"),Row(3,"Customer","Builder","","0","123.45")))).Save(output);
        }
        stream.Position=0;
        var result=JsonSerializer.SerializeToElement(await OpeningWorkbook.Read(stream));
        Assert.That(result.GetProperty("difference").GetDecimal(),Is.Zero);
        Assert.That(result.GetProperty("rows").GetArrayLength(),Is.EqualTo(2));
        Assert.That(result.GetProperty("errors")[0].GetString(),Does.Contain("Row 3"));
    }
}