using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using DocumentFormat.OpenXml.Drawing;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using WebAppSystems.Filters;
using WebAppSystems.Services;



namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class ProcessRecordController : Controller
    {
        private readonly ProcessRecordService _processRecordService;

        private readonly ClientService _clientService;

        private readonly AttorneyService _attorneyService;

        private readonly ValorClienteService _valorClienteService;

        private readonly IWebHostEnvironment _env;

        public ProcessRecordController(ProcessRecordService processRecordService, ClientService clientService, AttorneyService attorneyService, IWebHostEnvironment env,
            ValorClienteService valorClienteService)
        {
            _processRecordService = processRecordService;
            _clientService = clientService;
            _attorneyService = attorneyService;
            _valorClienteService = valorClienteService;
            _env = env;

        }

        public async Task<IActionResult> Index()
        {
            await PopulateViewBag();
            return View();
        }
        public async Task<IActionResult> SimpleSearch(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId)
        {
            SetDefaultDateValues(ref minDate, ref maxDate);

            PopulateViewData(minDate, maxDate, clientId, attorneyId);
            await PopulateViewBag();

            var result = await _processRecordService.FindByDateAsync(minDate, maxDate, clientId, attorneyId);
            return View(result);

        }

        // Ação para gerar e baixar o arquivo CSV

        public async Task<IActionResult> DownloadReport(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId, string format = "xlsx")
        {
            // Obter os registros filtrados usando a função FindByDateAsync
            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientId, attorneyId);

            string clientName = null;
            if (clientId.HasValue)
            {
                var client = await _clientService.FindByIdAsync(clientId.Value);
                if (client != null)
                {
                    clientName = client.Name;
                }
            }

            if (format == "csv")
            {
                // Construir o conteúdo do arquivo CSV
                StringBuilder csvContent = new StringBuilder();
                csvContent.AppendLine("Data;Usuario;Cliente;Atividade;Hora Inicio;Hora Final;Horas Trabalhadas;Area");
                foreach (var item in filteredRecords)
                {
                    csvContent.AppendLine($"{item.Date.ToString("dd/MM/yyyy")};{item.Attorney.Name};{item.Client.Name};{item.Description};{(int)item.HoraInicial.TotalHours}:{item.HoraInicial.Minutes:00};{(int)item.HoraFinal.TotalHours}:{item.HoraFinal.Minutes:00};{item.CalculoHoras()};{item.Department.Name}");
                }
                // Converter o conteúdo do CSV em bytes
                byte[] bytes = Encoding.GetEncoding("Windows-1252").GetBytes(csvContent.ToString());

                // Definir o nome do arquivo CSV para download
                string fileName = "exported_data.csv";

                // Retornar o arquivo CSV como resposta para download
                return File(bytes, "text/csv", fileName);
            }
            else if (format == "xlsx")
            {
                var workbook = new XSSFWorkbook();

                string startDateString = minDate?.ToString("ddMMyyyy") ?? "NoStart";
                string endDateString = maxDate?.ToString("ddMMyyyy") ?? "NoEnd";
                string sheetName = $"{startDateString}_{endDateString}";



                // Verifique se o nome da planilha é menor que 31 caracteres
                if (sheetName.Length > 31)
                {
                    sheetName = sheetName.Substring(0, 31);
                }
                var sheet = workbook.CreateSheet(sheetName);


                // Criar o estilo de sombreamento
                ICellStyle shadedStyle = workbook.CreateCellStyle();
                shadedStyle.FillForegroundColor = HSSFColor.Grey25Percent.Index;
                shadedStyle.FillPattern = FillPattern.SolidForeground;

                // Aplicar o estilo de sombreamento às células
                for (int i = 0; i <= 7; i++)
                {
                    IRow row = sheet.GetRow(i) ?? sheet.CreateRow(i);
                    for (int j = 0; j <= 8; j++)
                    {
                        ICell cell = row.GetCell(j) ?? row.CreateCell(j);
                        cell.CellStyle = shadedStyle;
                    }
                }

                // Criar o estilo de cabeçalho
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = HSSFColor.Black.Index;  // Definir a cor de fundo para preto
                headerStyle.FillPattern = FillPattern.SolidForeground;  // Padrão de preenchimento
                IFont font = workbook.CreateFont();
                font.Color = HSSFColor.White.Index;  // Definir a cor da fonte para branco
                headerStyle.SetFont(font);

                // Criar o cabeçalho na linha 8
                var headerRow = sheet.CreateRow(8);
                headerRow.CreateCell(0).SetCellValue("Data");
                headerRow.GetCell(0).CellStyle = headerStyle;
                headerRow.CreateCell(1).SetCellValue("Responsável");
                headerRow.GetCell(1).CellStyle = headerStyle;
                headerRow.CreateCell(2).SetCellValue("Solicitante");
                headerRow.GetCell(2).CellStyle = headerStyle;
                headerRow.CreateCell(3).SetCellValue("Cliente");
                headerRow.GetCell(3).CellStyle = headerStyle;
                headerRow.CreateCell(4).SetCellValue("Descrição");
                headerRow.GetCell(4).CellStyle = headerStyle;
                headerRow.CreateCell(5).SetCellValue("Hora Inicial");
                headerRow.GetCell(5).CellStyle = headerStyle;
                headerRow.CreateCell(6).SetCellValue("Hora Final");
                headerRow.GetCell(6).CellStyle = headerStyle;
                headerRow.CreateCell(7).SetCellValue("Horas Trabalhadas");
                headerRow.GetCell(7).CellStyle = headerStyle;
                headerRow.CreateCell(8).SetCellValue("Área");
                headerRow.GetCell(8).CellStyle = headerStyle;

                // Cria um estilo para as linhas sombreadas
                ICellStyle shadedRowStyle = workbook.CreateCellStyle();
                shadedRowStyle.FillForegroundColor = HSSFColor.Grey25Percent.Index; // Define a cor de fundo para cinza claro
                shadedRowStyle.FillPattern = FillPattern.SolidForeground; // Padrão de preenchimento

                // Adicionar dados ao arquivo Excel
                int rowNum = 9;  // Começando da linha 9, porque a primeira até a oitava são da imagem e o cabeçalho
                var rowTotal = sheet.CreateRow(rowNum);  // Crie a linha do total
                double totalHoras = 0;

                Dictionary<string, (double hours, double value)> departmentSummary = new Dictionary<string, (double hours, double value)>();

                for (int i = 0; i < filteredRecords.Count; i++)
                {
                    var item = filteredRecords[i];
                    var row = sheet.CreateRow(rowNum);

                    // Crie células para todas as colunas
                    for (int column = 0; column < 9; column++)
                    {
                        row.CreateCell(column);
                    }

                    row.GetCell(0).SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    row.GetCell(1).SetCellValue(item.Attorney.Name);
                    row.GetCell(2).SetCellValue(item.Solicitante);
                    row.GetCell(3).SetCellValue(item.Client.Name);
                    row.GetCell(4).SetCellValue(item.Description);
                    row.GetCell(5).SetCellValue(item.HoraInicial.ToString(@"hh\:mm"));
                    row.GetCell(6).SetCellValue(item.HoraFinal.ToString(@"hh\:mm"));
                    row.GetCell(7).SetCellValue(item.CalculoHoras());
                    //row.GetCell(7).SetCellValue(item.Department.Name);
                    string departmentName = item.Department != null ? item.Department.Name : "N/A";
                    row.GetCell(8).SetCellValue(departmentName);                 

                    totalHoras += item.CalculoHorasDecimal();

                    // Se o número da linha for ímpar, aplique o estilo de sombreamento
                    if (i % 2 != 0)
                    {
                        for (int j = 0; j < 9; j++)
                        {
                            row.GetCell(j).CellStyle = shadedRowStyle;
                        }
                    }


                    if (!departmentSummary.ContainsKey(departmentName))
                    {
                        departmentSummary[departmentName] = (0, 0);
                    }

                    double hours = item.CalculoHorasDecimal();

                    var valorCliente = await _valorClienteService.GetValorForClienteAndUserAsync(item.ClientId, item.Attorney.Id); // supondo que haja um método que retorna o valor baseado no Cliente e Usuario
                    double value = 0;
                    if (valorCliente != null)
                    {
                        double valuePerHour = valorCliente.Valor;
                        value = hours * valuePerHour;
                    }
                    departmentSummary[departmentName] = (departmentSummary[departmentName].hours + hours, departmentSummary[departmentName].value + value);

                    rowNum++;
                }

                // Define where the summary should start
                int summaryStartRow = rowNum + 2;

                // Create the header row for the summary
                IRow summaryHeaderRow = sheet.CreateRow(summaryStartRow);

                // Convertendo o total de horas em minutos e arredondando para o número mais próximo de minutos.
                totalHoras = Math.Round(totalHoras * 60) / 60;
                int totalMinutos = (int)Math.Round(totalHoras * 60);
                int horasInteiras = totalMinutos / 60;
                int minutosRestantes = totalMinutos % 60;

                string totalHorasFormatado = string.Format("{0}:{1:00}", horasInteiras, minutosRestantes);

                // Criação da linha com o total de horas
                var totalRow = sheet.CreateRow(rowNum);
                totalRow.CreateCell(0).SetCellValue("Total de horas");
                totalRow.GetCell(0).CellStyle = headerStyle;  // Sombreado em preto com fonte branca
                totalRow.CreateCell(7).SetCellValue(totalHorasFormatado);
                totalRow.GetCell(7).CellStyle = headerStyle;


                // Desativa as linhas de grade
                sheet.DisplayGridlines = false;

                for (int columnNum = 0; columnNum < 9; columnNum++)
                {
                    sheet.AutoSizeColumn(columnNum);
                }


                // Crie células para todas as colunas na linha de total
                for (int column = 0; column < 9; column++)
                {
                    totalRow.CreateCell(column);
                }

                totalRow.GetCell(0).SetCellValue("Total de horas");
                totalRow.GetCell(0).CellStyle = headerStyle;  // Sombreado em preto com fonte branca


                totalRow.GetCell(7).SetCellValue(totalHorasFormatado);
                totalRow.GetCell(7).CellStyle = headerStyle;  // Sombreado em preto com fonte branca

                // Aplicar o estilo de cabeçalho à linha de total
                for (int j = 0; j < 9; j++)
                {
                    totalRow.GetCell(j).CellStyle = headerStyle;
                }

                // Create the header row for the summary
                summaryHeaderRow = sheet.CreateRow(summaryStartRow);
                summaryHeaderRow.CreateCell(0).SetCellValue("Área");
                summaryHeaderRow.GetCell(0).CellStyle = headerStyle;
                summaryHeaderRow.CreateCell(1).SetCellValue("Horas");
                summaryHeaderRow.GetCell(1).CellStyle = headerStyle;
                summaryHeaderRow.CreateCell(2).SetCellValue("Valor");
                summaryHeaderRow.GetCell(2).CellStyle = headerStyle;

                // Print the summary data
                int summaryDataRow = summaryStartRow + 1;

                double totalHoursSummary = 0;
                double totalValueSummary = 0;

                CultureInfo brazilianCulture = new CultureInfo("pt-BR");
                foreach (var kvp in departmentSummary)
                {
                    IRow row = sheet.CreateRow(summaryDataRow);
                    row.CreateCell(0).SetCellValue(kvp.Key);
                    double hours = kvp.Value.hours;
                    totalHoursSummary += hours;  // add to total hours summary

                    // Convertendo o total de horas em minutos e arredondando para o número mais próximo de minutos.
                    int totalMinutes = (int)Math.Round(hours * 60);
                    int wholeHours = totalMinutes / 60;
                    int remainingMinutes = totalMinutes % 60;

                    string formattedHours = string.Format("{0}:{1:00}", wholeHours, remainingMinutes);


                    row.CreateCell(1).SetCellValue(formattedHours);

                    double value = kvp.Value.value;
                    totalValueSummary += value;  // add to total value summary
                                                 //row.CreateCell(2).SetCellValue(value);

                    row.CreateCell(2).SetCellValue(value.ToString("N2", brazilianCulture));
                    summaryDataRow++;
                }

                // Print the total summary
                IRow totalSummaryRow = sheet.CreateRow(summaryDataRow);
                totalSummaryRow.CreateCell(0).SetCellValue("Total");
                totalSummaryRow.GetCell(0).CellStyle = headerStyle;  // Apply the header style to total row

                double totalHours = Math.Round(totalHoursSummary * 60) / 60;
                int totalMinutesSummary = (int)Math.Round(totalHours * 60);
                int wholeHoursSummary = totalMinutesSummary / 60;
                int remainingMinutesSummary = totalMinutesSummary % 60;
                string formattedTotalHours = string.Format("{0}:{1:00}", wholeHoursSummary, remainingMinutesSummary);
                totalSummaryRow.CreateCell(1).SetCellValue(formattedTotalHours);
                totalSummaryRow.GetCell(1).CellStyle = headerStyle;  // Apply the header style to total row

                //totalSummaryRow.CreateCell(2).SetCellValue(totalValueSummary);
                totalSummaryRow.CreateCell(2).SetCellValue(totalValueSummary.ToString("N2", brazilianCulture));

                totalSummaryRow.GetCell(2).CellStyle = headerStyle;  // Apply the header style to total row

                var imagePath = System.IO.Path.Combine(_env.WebRootPath, "images", "LogoRelatorio.png");
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);

                int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);
                var helper = workbook.GetCreationHelper();
                var drawing = sheet.CreateDrawingPatriarch();
                var anchor = helper.CreateClientAnchor();
                // Defina a posição da imagem
                anchor.Col1 = 0;
                anchor.Row1 = 1;  // A imagem começa na segunda linha
                var picture = drawing.CreatePicture(anchor, pictureIdx);
                picture.Resize(7);  // A imagem vai ocupar 7 linhas


                // Adicionar a imagem do cliente ao relatório Excel
                byte[] clientImageData = null;
                string clientImageMimeType = null;
                if (clientId.HasValue)
                {
                    var client = await _clientService.FindByIdAsync(clientId.Value);
                    if (client != null)
                    {
                        clientImageData = client.ImageData;
                        clientImageMimeType = client.ImageMimeType;
                    }
                }

                
                if (clientImageData != null)
                {
                    var clientSheet = workbook.GetSheet(sheetName);
                    var clientDrawing = clientSheet.CreateDrawingPatriarch();  // Renomeie a variável para evitar conflito
                    var clientAnchor = helper.CreateClientAnchor();
                    clientAnchor.Col1 = 7;  // Inicia na coluna 8
                    clientAnchor.Row1 = 1;  // A imagem começa na segunda linha

                    // Adicionar a imagem do cliente à planilha
                    int clientPictureIdx = workbook.AddPicture(clientImageData, GetPictureType(clientImageMimeType));
                    var clientPicture = clientDrawing.CreatePicture(clientAnchor, clientPictureIdx);  
                    clientPicture.Resize(1);  // A imagem vai ocupar 3 colunas

                    // Ajuste da altura da imagem para ocupar até a linha 7
                    clientAnchor.Row2 = 6;  // Termina na linha 7
                    

                }

                string fileName = "Relatório_TimeSheet";
                if (!string.IsNullOrEmpty(clientName))
                {
                    fileName += $"_{clientName}";
                }
                fileName += ".xlsx";     




                // Para retornar como um arquivo para download
                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var content = stream.ToArray();

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            else
            {
                // Se o formato não for "csv" nem "xlsx", retorne um erro
                return BadRequest("Formato inválido");
            }
        }

        private PictureType GetPictureType(string mimeType)
        {
            switch (mimeType)
            {
                case "image/png":
                    return PictureType.PNG;
                case "image/jpeg":
                    return PictureType.JPEG;
                // Add more cases for other image types if needed
                default:
                    return PictureType.PNG; // Default to PNG if the type is not recognized
            }
        }


        #region Private Helpers

        private void SetDefaultDateValues(ref DateTime? minDate, ref DateTime? maxDate)
        {
            if (!minDate.HasValue)
            {
                minDate = new DateTime(DateTime.Now.Year, 1, 1);
            }
            if (!maxDate.HasValue)
            {
                maxDate = DateTime.Now;
            }
        }

        private void PopulateViewData(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId)
        {
            ViewData["minDate"] = minDate.Value.ToString("yyyy-MM-dd");
            ViewData["maxDate"] = maxDate.Value.ToString("yyyy-MM-dd");
            ViewData["clientId"] = clientId;
            ViewData["attorneyId"] = attorneyId;

        }

        private async Task PopulateViewBag()
        {
            ViewBag.Clients = await _clientService.FindAllAsync();
            ViewBag.Attorneys = await _attorneyService.FindAllAsync();
        }

        #endregion
    }
}

