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
using WebAppSystems.Helper;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Services;
using static WebAppSystems.Helper.Sessao;



namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
   
    public class ProcessRecordController : Controller
    {
        private readonly ProcessRecordService _processRecordService;

        private readonly ClientService _clientService;

        private readonly AttorneyService _attorneyService;

        private readonly ValorClienteService _valorClienteService;

        private readonly IWebHostEnvironment _env;

        private readonly ISessao _isessao;

        private readonly ParametroService _parametroService;

        public ProcessRecordController(ProcessRecordService processRecordService, ClientService clientService, AttorneyService attorneyService, IWebHostEnvironment env, ISessao isessao, 
            ValorClienteService valorClienteService, ParametroService parametroService)
        {
            _processRecordService = processRecordService;
            _clientService = clientService;
            _attorneyService = attorneyService;
            _valorClienteService = valorClienteService;
            _env = env;
            _isessao = isessao;
            _parametroService = parametroService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                await PopulateViewBag();
                return View();
            }
            catch (SessionExpiredException)
            {
                // Redirecione para a página de login se a sessão expirou
                TempData["MensagemAviso"] = "A sessão expirou. Por favor, faça login novamente.";
                return RedirectToAction("Index", "Login");
            }
        }
        public async Task<IActionResult> SimpleSearch(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId, string recordType)
        {
            SetDefaultDateValues(ref minDate, ref maxDate);

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            PopulateViewData(minDate, maxDate, clientId, attorneyId, recordTypeEnum.ToString());
            await PopulateViewBag();

            var result = await _processRecordService.FindByDateAsync(minDate, maxDate, clientId, attorneyId, recordTypeEnum);
            return View(result);

        }

        // Ação para gerar e baixar o arquivo CSV

        public async Task<IActionResult> DownloadReport(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId, string recordType = null, string format = "xlsx")
        {

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            // Obter os registros filtrados usando a função FindByDateAsync
            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientId, attorneyId, recordTypeEnum);

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

                // Criar o estilo de célula com quebra de texto
                ICellStyle cellStyle = workbook.CreateCellStyle();
                cellStyle.WrapText = true;

                // Criar o estilo de sombreamento com XSSF
                XSSFCellStyle shadedStyle = (XSSFCellStyle)workbook.CreateCellStyle();

                // Definindo a cor azul claro Ênfase 1 mais claro 80% em RGB
                XSSFColor lightBlueEmphasis = new XSSFColor(new byte[] { 222, 235, 247 });

                // Aplicar a cor ao estilo da célula
                shadedStyle.SetFillForegroundColor(lightBlueEmphasis);
                shadedStyle.FillPattern = FillPattern.SolidForeground;

                // Aplicar o estilo de sombreamento às células
                for (int i = 0; i <= 4; i++)
                {
                    IRow row = sheet.GetRow(i) ?? sheet.CreateRow(i);
                    for (int j = 0; j <= 9; j++)
                    {
                        ICell cell = row.GetCell(j) ?? row.CreateCell(j);
                        cell.CellStyle = shadedStyle; // Aplique o estilo com o azul claro ênfase 1
                    }
                }


                // Criar o estilo de cabeçalho
                ICellStyle headerStyle = workbook.CreateCellStyle();
                headerStyle.FillForegroundColor = IndexedColors.Black.Index;  // Definir a cor de fundo para preto
                headerStyle.FillPattern = FillPattern.SolidForeground;  // Padrão de preenchimento sólido

                // Criar a fonte para o cabeçalho
                IFont font = workbook.CreateFont();
                font.Color = IndexedColors.White.Index;  // Definir a cor da fonte para branco
                font.Boldweight = (short)FontBoldWeight.Bold;  // Deixar o texto em negrito
                headerStyle.SetFont(font);

                // Centralizar o texto no cabeçalho
                headerStyle.Alignment = HorizontalAlignment.Center;
                headerStyle.VerticalAlignment = VerticalAlignment.Center;

                // Criar o cabeçalho na linha 8
                var headerRow = sheet.CreateRow(5);

                // Criar as células do cabeçalho e aplicar o estilo
                string[] headers = { "Data", "Responsável", "Solicitante", "Cliente", "Tipo", "Descrição", "Hora Inicial", "Hora Final", "Horas Trabalhadas", "Área" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = headerRow.CreateCell(i);
                    cell.SetCellValue(headers[i]);
                    cell.CellStyle = headerStyle;
                }

                // Adicionar dados ao arquivo Excel
                int rowNum = 6;  // Começando da linha 9, porque a primeira até a oitava são da imagem e o cabeçalho
                var rowTotal = sheet.CreateRow(rowNum);  // Crie a linha do total
                double totalHoras = 0;

                Dictionary<string, (double hours, double value)> departmentSummary = new Dictionary<string, (double hours, double value)>();


                // Cria um estilo para as células com texto justificado e centralizado (para as outras colunas)
                ICellStyle justifiedCellStyle = workbook.CreateCellStyle();
                justifiedCellStyle.WrapText = true; // Permite a quebra de linha dentro da célula
                justifiedCellStyle.Alignment = HorizontalAlignment.Center; // Alinhamento central horizontal
                justifiedCellStyle.VerticalAlignment = VerticalAlignment.Center; // Alinhamento central vertical

                // Definindo a cor azul claro Ênfase 1 mais claro 80% em RGB
                //XSSFColor lightBlueEmphasis = new XSSFColor(new byte[] { 222, 235, 247 });

                // Cria um estilo que combina justificação, sombreamento e centralização (para as outras colunas)
                ICellStyle justifiedShadedStyle = workbook.CreateCellStyle();
                justifiedShadedStyle.CloneStyleFrom(justifiedCellStyle); // Copia as configurações de justificação e centralização
                ((XSSFCellStyle)justifiedShadedStyle).SetFillForegroundColor(lightBlueEmphasis); // Define a cor de fundo como azul claro Ênfase 1
                justifiedShadedStyle.FillPattern = FillPattern.SolidForeground; // Padrão de preenchimento sólido

                // Cria um estilo específico para a coluna 5 (alinhamento à esquerda e no topo)
                ICellStyle justifiedLeftTopStyle = workbook.CreateCellStyle();
                justifiedLeftTopStyle.WrapText = true; // Permite quebra de linha dentro da célula
                justifiedLeftTopStyle.Alignment = HorizontalAlignment.Left; // Alinhamento à esquerda
                justifiedLeftTopStyle.VerticalAlignment = VerticalAlignment.Top; // Alinhamento vertical no topo

                // Cria um estilo para a coluna 5 com sombreamento azul claro Ênfase 1
                ICellStyle justifiedLeftTopShadedStyle = workbook.CreateCellStyle();
                justifiedLeftTopShadedStyle.CloneStyleFrom(justifiedLeftTopStyle); // Copia o estilo de alinhamento à esquerda e no topo
                ((XSSFCellStyle)justifiedLeftTopShadedStyle).SetFillForegroundColor(lightBlueEmphasis); // Define a cor de fundo como azul claro Ênfase 1
                justifiedLeftTopShadedStyle.FillPattern = FillPattern.SolidForeground; // Padrão de preenchimento sólido



                // Definindo o tamanho da coluna 5 com uma largura fixa
                int columnIndex = 5; // coluna 5 (índice começa em 0)
                int columannWidth = 10000; // largura da coluna (o valor é em unidades de 1/256 da largura de um caractere)
                sheet.SetColumnWidth(columnIndex, columannWidth);


                for (int i = 0; i < filteredRecords.Count; i++)
                {
                    var item = filteredRecords[i];
                    var row = sheet.CreateRow(rowNum);

                    // Crie células para todas as colunas
                    for (int column = 0; column < 10; column++)
                    {
                        row.CreateCell(column);
                    }

                    row.GetCell(0).SetCellValue(item.Date.ToString("dd/MM/yyyy"));
                    row.GetCell(1).SetCellValue(item.Attorney.Name);
                    row.GetCell(2).SetCellValue(item.Solicitante);
                    row.GetCell(3).SetCellValue(item.Client.Name);
                    row.GetCell(4).SetCellValue(item.RecordType.ToString());
                    // row.GetCell(5).SetCellValue(item.Description);

                    // Definindo o valor da célula na coluna 5 e aplicando o estilo justificado à esquerda e no topo
                    ICell descriptionCell = row.GetCell(columnIndex);
                    descriptionCell.SetCellValue(item.Description);

                    row.GetCell(6).SetCellValue(item.HoraInicial.ToString(@"hh\:mm"));
                    row.GetCell(7).SetCellValue(item.HoraFinal.ToString(@"hh\:mm"));
                    row.GetCell(8).SetCellValue(item.CalculoHoras());
                    //row.GetCell(7).SetCellValue(item.Department.Name);
                    string departmentName = item.Department != null ? item.Department.Name : "N/A";
                    row.GetCell(9).SetCellValue(departmentName);

                    totalHoras += item.CalculoHorasDecimal();


                    // Aplique o estilo correto: justificado com ou sem sombreamento, dependendo se a linha é ímpar ou par
                    for (int j = 0; j < 10; j++)
                    {
                        if (i % 2 != 0) // Linhas ímpares
                        {
                            if (j == 5) // Coluna 5 com alinhamento à esquerda e no topo com sombreamento
                            {
                                row.GetCell(j).CellStyle = justifiedLeftTopShadedStyle;
                            }
                            else // Outras colunas com sombreamento e centralizadas
                            {
                                row.GetCell(j).CellStyle = justifiedShadedStyle;
                            }
                        }
                        else // Linhas pares
                        {
                            if (j == 5) // Coluna 5 com alinhamento à esquerda e no topo sem sombreamento
                            {
                                row.GetCell(j).CellStyle = justifiedLeftTopStyle;
                            }
                            else // Outras colunas sem sombreamento e centralizadas
                            {
                                row.GetCell(j).CellStyle = justifiedCellStyle;
                            }
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

                //   for (int columnNum = 0; columnNum < 10; columnNum++)
                //   {
                //       sheet.AutoSizeColumn(columnNum);
                //   }

                for (int columnNum = 0; columnNum < 10; columnNum++)
                {
                    if (columnNum != 5) // Não aplicar AutoSize na coluna 5
                    {
                        sheet.AutoSizeColumn(columnNum);
                    }
                }


                // Crie células para todas as colunas na linha de total
                for (int column = 0; column < 10; column++)
                {
                    totalRow.CreateCell(column);
                }

                totalRow.GetCell(0).SetCellValue("Total de horas");
                totalRow.GetCell(0).CellStyle = headerStyle;  // Sombreado em preto com fonte branca


                totalRow.GetCell(8).SetCellValue(totalHorasFormatado);
                totalRow.GetCell(8).CellStyle = headerStyle;  // Sombreado em preto com fonte branca

                // Aplicar o estilo de cabeçalho à linha de total
                for (int j = 0; j < 10; j++)
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
                /*
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
                picture.Resize(4);  // A imagem vai ocupar 7 linhas

                */
                var (imageBytes, mimeType, width, height) = await _parametroService.GetLogoAsync();

                int pictureIdx = workbook.AddPicture(imageBytes, PictureType.PNG);
                var helper = workbook.GetCreationHelper();
                var drawing = sheet.CreateDrawingPatriarch();
                var anchor = helper.CreateClientAnchor();

                // Defina a posição da imagem e ajuste o tamanho conforme as configurações
                anchor.Col1 = 0; // Defina a coluna inicial
                anchor.Row1 = 1; // Defina a linha inicial
                anchor.Col2 = anchor.Col1 + width; // Defina a coluna final com base na largura do logo
                anchor.Row2 = anchor.Row1 + height; // Defina a linha final com base na altura do logo

                var picture = drawing.CreatePicture(anchor, pictureIdx);
                picture.Resize(4);



                // Criar o estilo para a palavra "TimeSheet" com fonte de tamanho 30 e negrito
                ICellStyle timeSheetStyle = workbook.CreateCellStyle();
                IFont font1 = workbook.CreateFont();
                font1.FontHeightInPoints = 30;  // Define o tamanho da fonte como 30
                font1.Boldweight = (short)FontBoldWeight.Bold; // Define a fonte como negrito
                timeSheetStyle.SetFont(font1);

                // Centralizar o texto na célula
                timeSheetStyle.Alignment = HorizontalAlignment.Center;
                timeSheetStyle.VerticalAlignment = VerticalAlignment.Center;

                // Adicionar a palavra "TimeSheet" na célula (coluna 6, linha 3)
                var row3 = sheet.GetRow(2) ?? sheet.CreateRow(2); // Linha 3 é índice 2 (começa do 0)
                var cell3 = row3.GetCell(5) ?? row3.CreateCell(5); // Coluna 6 é índice 5
                cell3.SetCellValue("TimeSheet");
                cell3.CellStyle = timeSheetStyle; // Aplicar o estilo à célula

                // Agora copiar o estilo de sombreamento, se necessário
                var previousRow = sheet.GetRow(1); // Pega a linha 2 para copiar o estilo de uma célula
                if (previousRow != null)
                {
                    var previousCell = previousRow.GetCell(5); // Pega a célula da mesma coluna
                    if (previousCell != null)
                    {
                        var previousStyle = previousCell.CellStyle;
                        if (previousStyle != null)
                        {
                            // Clonar o estilo de sombreamento sem afetar a fonte
                            ICellStyle clonedStyle = workbook.CreateCellStyle();
                            clonedStyle.CloneStyleFrom(previousStyle);
                            clonedStyle.SetFont(font1); // Manter a fonte definida
                            clonedStyle.Alignment = HorizontalAlignment.Center; // Manter centralizado
                            clonedStyle.VerticalAlignment = VerticalAlignment.Center; // Manter centralizado
                            cell3.CellStyle = clonedStyle; // Aplicar o estilo clonado à célula
                        }
                    }
                }


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

        private void PopulateViewData(DateTime? minDate, DateTime? maxDate, int? clientId, int? attorneyId, string recordType)
        {
            ViewData["minDate"] = minDate.Value.ToString("yyyy-MM-dd");
            ViewData["maxDate"] = maxDate.Value.ToString("yyyy-MM-dd");
            ViewData["clientId"] = clientId;
            ViewData["attorneyId"] = attorneyId;
            ViewData["selectedRecordType"] = recordType;

        }

        private async Task PopulateViewBag()
        {
            ViewBag.Clients = await _clientService.FindAllAsync();
            ViewBag.Attorneys = await _attorneyService.FindAllAsync();

            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.UserProfile = usuario.Perfil;        
        }

        #endregion
    }
}

