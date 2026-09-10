using Core;
using System.Text.Json;
using System.Text.Encodings.Web;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    };
    string json = JsonSerializer.Serialize(report, options);
    Console.WriteLine(json);
}
else
{
    Console.WriteLine("CrossApp – інформація про середовище");
    Console.WriteLine("Студент: Столярська Яна, група ФЕІ-34");
    Console.WriteLine("Предметна область: Бібліотека (книги, примірники, читачі, видачі)");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"ОС              : {report.OsDescription}");
    Console.WriteLine($"Runtime         : {report.FrameworkDescription}");
    Console.WriteLine($"Архітектура     : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (визначено) : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)  : {report.ReportedRid}");
    Console.WriteLine($"Каталог         : {report.BaseDirectory}");
    Console.WriteLine($"Примітка збірки : {report.BuildNote}");
}