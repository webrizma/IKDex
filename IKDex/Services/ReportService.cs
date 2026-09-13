using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace IKDex.Services;

public sealed class ReportService(DatabaseService databaseService)
{
    public (string Title, string[] Headers, List<string[]> Rows) Build(string reportType, DateTime month)
    {
        return reportType switch
        {
            "Personel Listesi" => BuildEmployees(),
            "İzin Raporu" => BuildLeaves(month),
            "Puantaj Raporu" => BuildAttendance(month),
            _ => throw new ArgumentOutOfRangeException(nameof(reportType))
        };
    }

    public void ExportExcel(string path, string reportType, DateTime month)
    {
        var report=Build(reportType,month); using var workbook=new XLWorkbook(); var sheet=workbook.Worksheets.Add("Rapor");
        sheet.Cell(1,1).Value="IKDex"; sheet.Cell(1,1).Style.Font.FontSize=18; sheet.Cell(1,1).Style.Font.Bold=true; sheet.Cell(1,1).Style.Font.FontColor=XLColor.FromHtml("#5B5BD6");
        sheet.Cell(2,1).Value=report.Title; sheet.Cell(3,1).Value=$"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm}"; sheet.Cell(3,1).Style.Font.FontColor=XLColor.Gray;
        for(var col=0;col<report.Headers.Length;col++)sheet.Cell(5,col+1).Value=report.Headers[col];
        var header=sheet.Range(5,1,5,report.Headers.Length); header.Style.Fill.BackgroundColor=XLColor.FromHtml("#24245D");header.Style.Font.FontColor=XLColor.White;header.Style.Font.Bold=true;
        for(var row=0;row<report.Rows.Count;row++)for(var col=0;col<report.Headers.Length;col++)sheet.Cell(row+6,col+1).Value=report.Rows[row][col];
        if(report.Rows.Count>0){var table=sheet.Range(5,1,report.Rows.Count+5,report.Headers.Length);table.Style.Border.BottomBorder=XLBorderStyleValues.Thin;table.Style.Border.BottomBorderColor=XLColor.FromHtml("#E5E8EE");}
        sheet.SheetView.FreezeRows(5);sheet.Columns().AdjustToContents();foreach(var column in sheet.ColumnsUsed())if(column.Width>40)column.Width=40;
        workbook.SaveAs(path);
    }

    public void ExportPdf(string path,string reportType,DateTime month)
    {
        var report=Build(reportType,month);using var document=new PdfDocument();document.Info.Title=report.Title;
        var titleFont=new XFont("Segoe UI",16,XFontStyleEx.Bold);var headerFont=new XFont("Segoe UI",8,XFontStyleEx.Bold);var textFont=new XFont("Segoe UI",8);
        PdfPage? page=null;XGraphics? graphics=null;double y=0;double[] widths=Enumerable.Repeat(0.0,report.Headers.Length).ToArray();var available=770.0;for(var i=0;i<widths.Length;i++)widths[i]=available/widths.Length;
        void NewPage(){graphics?.Dispose();page=document.AddPage();page.Orientation=PdfSharp.PageOrientation.Landscape;graphics=XGraphics.FromPdfPage(page);y=38;graphics.DrawString("IKDex",titleFont,new XSolidBrush(XColor.FromArgb(91,91,214)),40,y);graphics.DrawString(report.Title,new XFont("Segoe UI",11,XFontStyleEx.Bold),XBrushes.Black,130,y);y+=26;DrawRow(report.Headers,true);}
        void DrawRow(string[] cells,bool isHeader){var x=40.0;var height=22.0;for(var i=0;i<cells.Length;i++){if(isHeader)graphics!.DrawRectangle(new XSolidBrush(XColor.FromArgb(36,36,93)),x,y,widths[i],height);else graphics!.DrawRectangle(new XPen(XColor.FromArgb(225,229,236),0.5),x,y,widths[i],height);var value=cells[i].Length>28?cells[i][..27]+"…":cells[i];graphics!.DrawString(value,isHeader?headerFont:textFont,isHeader?XBrushes.White:XBrushes.Black,new XRect(x+4,y,widths[i]-8,height),XStringFormats.CenterLeft);x+=widths[i];}y+=height;}
        NewPage();foreach(var row in report.Rows){if(y>540)NewPage();DrawRow(row,false);}graphics?.DrawString($"Toplam {report.Rows.Count} kayıt - {DateTime.Now:dd.MM.yyyy HH:mm}",textFont,XBrushes.Gray,40,570);graphics?.Dispose();document.Save(path);
    }

    private (string,string[],List<string[]>) BuildEmployees(){var rows=new EmployeeService(databaseService).GetAll(null,true).Select(e=>new[]{e.EmployeeNumber,e.FullName,e.Department,e.Position,e.Email,e.Phone,e.StartDate.ToString("dd.MM.yyyy"),e.StatusText}).ToList();return("Personel Listesi",new[]{"Sicil No","Ad Soyad","Departman","Pozisyon","E-posta","Telefon","Başlangıç","Durum"},rows);}
    private (string,string[],List<string[]>) BuildLeaves(DateTime month){var rows=new LeaveService(databaseService).GetAll().Where(x=>x.StartDate.Year==month.Year&&x.StartDate.Month==month.Month).Select(x=>new[]{x.EmployeeName,x.LeaveType,x.StartDate.ToString("dd.MM.yyyy"),x.EndDate.ToString("dd.MM.yyyy"),x.DayCount.ToString(),x.Status,x.Reason}).ToList();return($"İzin Raporu - {month:MMMM yyyy}",new[]{"Personel","İzin Türü","Başlangıç","Bitiş","İş Günü","Durum","Açıklama"},rows);}
    private (string,string[],List<string[]>) BuildAttendance(DateTime month){var rows=new AttendanceService(databaseService).GetMonth(month).Select(x=>new[]{x.WorkDate.ToString("dd.MM.yyyy"),x.EmployeeName,x.CheckInText,x.CheckOutText,x.WorkedText,x.Note}).ToList();return($"Puantaj Raporu - {month:MMMM yyyy}",new[]{"Tarih","Personel","Giriş","Çıkış","Çalışma","Not"},rows);}
}
