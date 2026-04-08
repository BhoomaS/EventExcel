using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Snowflake.Data.Client;
using OfficeOpenXml;
using System.Linq;
using System.Drawing;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.Drawing.Chart;
using MemberSummary.Models;
using System.Diagnostics;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;


[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async Task<IActionResult> GenerateSummaryReport([FromBody] SummaryRequest request)
    {
        try
        {
            Console.WriteLine("GenerateSummaryReport called");

            if (string.IsNullOrWhiteSpace(request.ClientName) ||
                string.IsNullOrWhiteSpace(request.ClientId) ||
                string.IsNullOrWhiteSpace(request.MemberId))
            {
                return BadRequest("Missing required parameters.");
            }

            string chMemberId = request.MemberId;

            // Create temp path under wwwroot/temp (make sure this folder exists and is writable)
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "temp");
            if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

            string tempFilePath = Path.Combine(rootPath, $"{chMemberId}_MemberSummary.xlsx");

            // Snowflake connection string - update path based on live server
            string connectionString = "account=hr94994;authenticator=SNOWFLAKE_JWT;" +
                                      "private_key_file=C:\\Reports\\key\\GlueServiceAccess.p8;" +
                                      "private_key_pwd=GlueServiceAccess01;" +
                                      "db=DATA_WAREHOUSE_DEV;schema=PORTAL;role=CHDATA;user=GLUEUSER;";

            using (IDbConnection conn = new SnowflakeDbConnection { ConnectionString = connectionString })
            {
                conn.Open();

                var storedProcedures = new List<string>
            {
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_RISK_SCORES_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_MONTHLY_SPEND_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_DIAGNOSIS_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_PROCEDURE_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_PROVIDER_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_DRUGS_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_HOSPITALIZATIONS_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_RECOMMENDATIONS_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_UTILIZATIONS_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());",
                $"CALL DATA_WAREHOUSE_DEV.LOAD.SP_MEMBERSUMMARY_EVENT_GRAPH_UPDATED_QC('{request.ClientName}', '{request.ClientId}', '{chMemberId}', CURRENT_USER());"

            };

                foreach (var sp in storedProcedures)
                {
                    ExecuteCommand(conn, sp);
                }

                var queries = new Dictionary<string, string>
            {
                { "RiskScore", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_RISK_SCORES_REPORT order by DATE desc" },
                { "MonthlySpend", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_MONTHLY_SPEND_REPORT" },
                { "Diagnosis", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_DIAGNOSIS_REPORT" },
                { "Procedure", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_PROCEDURE_REPORT order by LAST_CLAIM_DATE desc" },
                { "Provider", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_PROVIDER_REPORT order by LAST_CLAIM_DATE desc" },
                { "Drugs", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_DRUGS_REPORT where DRUG_NAME is not null order by LAST_CLAIM_DATE desc" },
                { "Hospitalizations", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBERSUMMARY_HOSPITALIZATIONS_REPORT" },
                { "Recommendations", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBERSUMMARY_RECOMMENDATIONS_REPORT order by RECOMMENDATION_DATE desc" },
                { "Utilizations", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBER_SUMMARY_UTILIZATIONS_REPORT" },
                { "EventGraph", "SELECT * FROM DATA_WAREHOUSE_DEV.LOAD.QC_MEMBERSUMMARY_EVENT_GRAPH_REPORT order by DATE_OF_SERVICE desc" }
            };

                SaveDataToExcel(conn, queries, tempFilePath);
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
            string fileName = $"{chMemberId}_MemberSummary.xlsx";

            System.IO.File.Delete(tempFilePath);

            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating member summary report.");
            return BadRequest($"An error occurred: {ex.Message}");
        }
    }




    [HttpPost]
    public async Task<IActionResult> GenerateSummaryFileReport(IFormFile file, string clientName, string clientId)
    {
        
        if (file == null || file.Length == 0)
        {
            return BadRequest("File is empty.");
        }

        List<string> memberIds = new List<string>();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var package = new ExcelPackage(stream))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++) // Assuming header is on row 1
                {
                    string memberId = worksheet.Cells[row, 1].Text;
                    if (!string.IsNullOrWhiteSpace(memberId))
                    {
                        memberIds.Add(memberId);
                    }
                }
            }
        }

        return Json(new { clientName, clientId, memberIds });
    }


    // Helper Methods
    static void ExecuteCommand(IDbConnection conn, string commandText)
    {
        using (IDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = commandText;
            cmd.ExecuteNonQuery();
        }
    }

    static DataTable FetchData(IDbConnection conn, string query)
    {
        using (IDbCommand cmd = conn.CreateCommand())
        {
            cmd.CommandText = query;
            using (IDataReader reader = cmd.ExecuteReader())
            {
                DataTable dataTable = new DataTable();
                dataTable.Load(reader);
                return dataTable;
            }
        }
    }

    static void SaveDataToExcel(IDbConnection conn, Dictionary<string, string> queries, string filePath)
    {
        using (var workbook = new XLWorkbook())
        {
            foreach (var kvp in queries)
            {
                string sheetName = kvp.Key;
                string query = kvp.Value;
                DataTable dataTable = FetchData(conn, query);
                var sheet = workbook.Worksheets.Add(sheetName);
                if (dataTable.Rows.Count > 0 || dataTable.Columns.Count > 0)
                    sheet.Cell(1, 1).InsertTable(dataTable);
            }

            if (workbook.Worksheets.Count > 0)
            {
                workbook.SaveAs(filePath);
            }
        }

        // Adding charts to the Excel file
        var file = new FileInfo(filePath);
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using (var package = new ExcelPackage(file))
        {
            var workbook = package.Workbook;
            if (workbook.Worksheets.Count == 0)
                return;
            var worksheet = workbook.Worksheets[0];
            if (worksheet.Dimension == null)
            {
                Console.WriteLine("Worksheet is empty. No data to create a chart.");
                return;
            }
            int lastRow = worksheet.Dimension.End.Row;
            var chart = worksheet.Drawings.AddChart("chart1", eChartType.ColumnClustered);
            chart.Title.Text = "RiskScore Graph";

            string xRange = $"'{worksheet.Name}'!B2:B{lastRow}";
            string totalRisk = $"'{worksheet.Name}'!C2:C{lastRow}";
            string absoluteRisk = $"'{worksheet.Name}'!D2:D{lastRow}";
            string careQualityRisk = $"'{worksheet.Name}'!E2:E{lastRow}";
            string flareRisk = $"'{worksheet.Name}'!F2:F{lastRow}";

            var totalRiskScore = chart.Series.Add(totalRisk, xRange);
            totalRiskScore.Header = "Total Risk Score";
            totalRiskScore.Fill.Color = ColorTranslator.FromHtml("#EECC54");

            var absoluteRiskScore = chart.Series.Add(absoluteRisk, xRange);
            absoluteRiskScore.Header = "Absolute Risk Score";
            absoluteRiskScore.Fill.Color = ColorTranslator.FromHtml("#C1DEDC");

            var careQualityRiskScore = chart.Series.Add(careQualityRisk, xRange);
            careQualityRiskScore.Header = "Care Quality Risk Score";
            careQualityRiskScore.Fill.Color = ColorTranslator.FromHtml("#8EAAC6");

            var flareRiskScore = chart.Series.Add(flareRisk, xRange);
            flareRiskScore.Header = "Flare Risk Score";
            flareRiskScore.Fill.Color = ColorTranslator.FromHtml("#BD91B1");
            chart.SetPosition(0, 8, 8, 10);
            chart.SetSize(1800, 500);
            package.Save();
        }

        using (var package = new ExcelPackage(file))
        {
            var workbook = package.Workbook;
            // Second chart is for MonthlySpend sheet; only add if that worksheet exists (0-based index 1)
            if (workbook.Worksheets.Count < 2)
                return;
            var worksheet = workbook.Worksheets[1];
            if (worksheet.Dimension == null)
            {
                Console.WriteLine("Worksheet is empty. No data to create a chart.");
                return;
            }
            int lastRow = worksheet.Dimension.End.Row;
            var chart = worksheet.Drawings.AddChart("chart1", eChartType.ColumnClustered);
            chart.Title.Text = "Monthly Spend";

            string xRange = $"'{worksheet.Name}'!B2:B{lastRow}";
            string totalAmt = $"'{worksheet.Name}'!C2:C{lastRow}";
            string medicalAmt = $"'{worksheet.Name}'!D2:D{lastRow}";
            string pharmacyAmt = $"'{worksheet.Name}'!E2:E{lastRow}";

            // Add series with custom colors
            var totalValue = chart.Series.Add(totalAmt, xRange);
            totalValue.Header = "Total Amount";
            totalValue.Fill.Color = ColorTranslator.FromHtml("#EECC54");

            var medicalValue = chart.Series.Add(medicalAmt, xRange);
            medicalValue.Header = "Medical Amount";
            medicalValue.Fill.Color = ColorTranslator.FromHtml("#C1DEDC");

            var pharmacyValue = chart.Series.Add(pharmacyAmt, xRange);
            pharmacyValue.Header = "Pharmacy Amount";
            pharmacyValue.Fill.Color = ColorTranslator.FromHtml("#8EAAC6");
            chart.SetPosition(0, 8, 8, 10);
            chart.SetSize(1800, 500);
            package.Save();
        }

    }

    public class SummaryRequest
    {
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public string MemberId { get; set; }
    }



}
