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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NPOIHorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;
using NPOIVerticalAlignment = NPOI.SS.UserModel.VerticalAlignment;



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

        private readonly DepartmentService _departmentService;


        public ProcessRecordController(ProcessRecordService processRecordService, ClientService clientService, AttorneyService attorneyService, IWebHostEnvironment env, ISessao isessao, 
            ValorClienteService valorClienteService, ParametroService parametroService, DepartmentService departmentService)
        {
            _processRecordService = processRecordService;
            _clientService = clientService;
            _attorneyService = attorneyService;
            _valorClienteService = valorClienteService;
            _env = env;
            _isessao = isessao;
            _parametroService = parametroService;
            _departmentService = departmentService;
            
            // Configurar licença do QuestPDF (Community License)
            QuestPDF.Settings.License = LicenseType.Community;
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
        public async Task<IActionResult> SimpleSearch(DateTime? minDate, DateTime? maxDate, string clientIds, int? attorneyId, int? departmentId, string recordType)
        {
            SetDefaultDateValues(ref minDate, ref maxDate);

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            // Converter string de IDs separados por vírgula em lista de inteiros
            List<int> clientIdList = null;
            if (!string.IsNullOrEmpty(clientIds))
            {
                clientIdList = clientIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }

            PopulateViewData(minDate, maxDate, clientIdList, attorneyId, recordTypeEnum?.ToString());
            await PopulateViewBag();

            var result = await _processRecordService.FindByDateAsync(minDate, maxDate, clientIdList, attorneyId, departmentId, recordTypeEnum);
            return View(result);

        }

        // Ação para gerar e baixar o arquivo CSV

        public async Task<IActionResult> DownloadReport(DateTime? minDate, DateTime? maxDate, string clientIds, int? attorneyId, int? departmentId, string recordType = null, string format = "xlsx")
        {

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            // Converter string de IDs separados por vírgula em lista de inteiros
            List<int> clientIdList = null;
            if (!string.IsNullOrEmpty(clientIds))
            {
                clientIdList = clientIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }

            // Obter os registros filtrados usando a função FindByDateAsync
            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientIdList, attorneyId, departmentId, recordTypeEnum);

            string clientName = null;
            if (clientIdList != null && clientIdList.Count == 1)
            {
                var client = await _clientService.FindByIdAsync(clientIdList.First());
                if (client != null)
                {
                    clientName = client.Name;
                }
            }
            else if (clientIdList != null && clientIdList.Count > 1)
            {
                clientName = "Multiplos_Clientes";
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

                // Cor azul claro para linhas alternadas de dados
                XSSFColor lightBlueEmphasis = new XSSFColor(new byte[] { 222, 235, 247 });
                XSSFCellStyle shadedStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                shadedStyle.SetFillForegroundColor(lightBlueEmphasis);
                shadedStyle.FillPattern = FillPattern.SolidForeground;

                // Estilo da célula mesclada do cabeçalho: azul claro sólido, sem bordas
                XSSFCellStyle headerBgStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                headerBgStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 189, 215, 238 }));
                headerBgStyle.FillPattern = FillPattern.SolidForeground;
                headerBgStyle.BorderTop = BorderStyle.None;
                headerBgStyle.BorderBottom = BorderStyle.None;
                headerBgStyle.BorderLeft = BorderStyle.None;
                headerBgStyle.BorderRight = BorderStyle.None;

                // Criar as linhas 0-4 e preencher todas as células com a cor do cabeçalho
                for (int i = 0; i <= 4; i++)
                {
                    IRow row = sheet.GetRow(i) ?? sheet.CreateRow(i);
                    for (int j = 0; j <= 9; j++)
                    {
                        ICell cell = row.GetCell(j) ?? row.CreateCell(j);
                        cell.CellStyle = headerBgStyle;
                    }
                }

                // Mesclar toda a região do cabeçalho (linhas 0-4, colunas 0-9)
                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 4, 0, 9));


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
                headerStyle.Alignment = NPOIHorizontalAlignment.Center;
                headerStyle.VerticalAlignment = NPOIVerticalAlignment.Center;

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
                justifiedCellStyle.Alignment = NPOIHorizontalAlignment.Center; // Alinhamento central horizontal
                justifiedCellStyle.VerticalAlignment = NPOIVerticalAlignment.Center; // Alinhamento central vertical

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
                justifiedLeftTopStyle.Alignment = NPOIHorizontalAlignment.Left; // Alinhamento à esquerda
                justifiedLeftTopStyle.VerticalAlignment = NPOIVerticalAlignment.Top; // Alinhamento vertical no topo

                // Cria um estilo para a coluna 5 com sombreamento azul claro Ênfase 1
                ICellStyle justifiedLeftTopShadedStyle = workbook.CreateCellStyle();
                justifiedLeftTopShadedStyle.CloneStyleFrom(justifiedLeftTopStyle); // Copia o estilo de alinhamento à esquerda e no topo
                ((XSSFCellStyle)justifiedLeftTopShadedStyle).SetFillForegroundColor(lightBlueEmphasis); // Define a cor de fundo como azul claro Ênfase 1
                justifiedLeftTopShadedStyle.FillPattern = FillPattern.SolidForeground; // Padrão de preenchimento sólido



                // Definindo o tamanho da coluna 5 com uma largura fixa
                int columnIndex = 5; // coluna 5 (índice começa em 0)
                int columannWidth = 10000; // largura da coluna (o valor é em unidades de 1/256 da largura de um caractere)
                sheet.SetColumnWidth(columnIndex, columannWidth);


                for (int i = 0; i < filteredRecords.Count(); i++)
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

                    // Verifica se o tipo de registro é "Deslocamento" para considerar apenas 50% das horas
                    double horasCalculadas = item.CalculoHorasDecimal();
                    if (item.RecordType.ToString().Equals("Deslocamento", StringComparison.OrdinalIgnoreCase))
                    {
                        horasCalculadas *= 0.5;
                    }

                    // Converter horas decimais para formato hh:mm
                    int horasItem = (int)horasCalculadas;
                    int minutosItem = (int)Math.Round((horasCalculadas - horasItem) * 60);
                    string horasFormatadas = string.Format("{0}:{1:00}", horasItem, minutosItem);
                    row.GetCell(8).SetCellValue(horasFormatadas);


                    //row.GetCell(7).SetCellValue(item.Department.Name);
                    string departmentName = item.Department != null ? item.Department.Name : "N/A";
                    row.GetCell(9).SetCellValue(departmentName);

                    //totalHoras += item.CalculoHorasDecimal();

                    double totalHorasCalculadas = item.CalculoHorasDecimal();

                    // Se for "Deslocamento", aplica 50% apenas para esse item
                    if (item.RecordType.ToString().Equals("Deslocamento", StringComparison.OrdinalIgnoreCase))
                    {
                        totalHorasCalculadas *= 0.5;
                    }

                    // Adiciona ao total corretamente
                    totalHoras += totalHorasCalculadas;





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

                    if (item.RecordType.ToString().Equals("Deslocamento", StringComparison.OrdinalIgnoreCase))
                    {
                        hours *= 0.5;
                    }

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

                anchor.Col1 = 0;
                anchor.Row1 = 1;
                anchor.Col2 = anchor.Col1 + width;
                anchor.Row2 = anchor.Row1 + height;

                var picture = drawing.CreatePicture(anchor, pictureIdx);
                picture.Resize(4);



                // Criar o estilo para a palavra "TimeSheet" com fonte de tamanho 30 e negrito
                ICellStyle timeSheetStyle = workbook.CreateCellStyle();
                IFont font1 = workbook.CreateFont();
                font1.FontHeightInPoints = 30;  // Define o tamanho da fonte como 30
                font1.Boldweight = (short)FontBoldWeight.Bold; // Define a fonte como negrito
                timeSheetStyle.SetFont(font1);

                // Centralizar o texto na célula
                timeSheetStyle.Alignment = NPOIHorizontalAlignment.Center;
                timeSheetStyle.VerticalAlignment = NPOIVerticalAlignment.Center;

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
                            clonedStyle.Alignment = NPOIHorizontalAlignment.Center; // Manter centralizado
                            clonedStyle.VerticalAlignment = NPOIVerticalAlignment.Center; // Manter centralizado
                            cell3.CellStyle = clonedStyle; // Aplicar o estilo clonado à célula
                        }
                    }
                }


                // Adicionar a imagem do cliente ao relatório Excel
                byte[] clientImageData = null;
                string clientImageMimeType = null;
                if (clientIdList != null && clientIdList.Count == 1)
                {
                    var client = await _clientService.FindByIdAsync(clientIdList.First());
                    if (client != null)
                    {
                        clientImageData = client.ImageData;
                        clientImageMimeType = client.ImageMimeType;
                    }
                }

                
                if (clientImageData != null)
                {
                    var clientSheet = workbook.GetSheet(sheetName);
                    var clientDrawing = clientSheet.CreateDrawingPatriarch();
                    var clientAnchor = helper.CreateClientAnchor();

                    // Logo do cliente: fixado nas linhas 1-5, colunas 7-9
                    clientAnchor.AnchorType = AnchorType.MoveAndResize;
                    clientAnchor.Col1 = 7;
                    clientAnchor.Row1 = 0;
                    clientAnchor.Col2 = 10;
                    clientAnchor.Row2 = 5;
                    clientAnchor.Dx1 = 0;
                    clientAnchor.Dy1 = 0;
                    clientAnchor.Dx2 = 0;
                    clientAnchor.Dy2 = 0;

                    int clientPictureIdx = workbook.AddPicture(clientImageData, GetPictureType(clientImageMimeType));
                    var clientPicture = clientDrawing.CreatePicture(clientAnchor, clientPictureIdx);
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

                    // Injetar gradiente horizontal (branco → azul) na célula mesclada do cabeçalho via OpenXML
                    content = InjectGradientFill(content);

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

        // Ação para gerar e baixar o arquivo PDF
        public async Task<IActionResult> DownloadPdfReport(DateTime? minDate, DateTime? maxDate, string clientIds, int? attorneyId, int? departmentId, string recordType = null)
        {
            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            // Converter string de IDs separados por vírgula em lista de inteiros
            List<int> clientIdList = null;
            if (!string.IsNullOrEmpty(clientIds))
            {
                clientIdList = clientIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }

            // Obter os registros filtrados
            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientIdList, attorneyId, departmentId, recordTypeEnum);

            // Calcular total de horas
            TimeSpan totalHours = TimeSpan.Zero;
            foreach (var item in filteredRecords)
            {
                totalHours += item.CalculoHorasTotal();
            }
            int totalDays = (int)totalHours.TotalDays;
            TimeSpan correctedTotalHours = totalHours - TimeSpan.FromDays(totalDays);
            string totalFormatted = $"{totalDays * 24 + correctedTotalHours.Hours}:{correctedTotalHours.Minutes:00}";

            // Nome do cliente para o arquivo
            string clientName = "Todos";
            if (clientIdList != null && clientIdList.Count == 1)
            {
                var client = await _clientService.FindByIdAsync(clientIdList.First());
                if (client != null)
                {
                    clientName = client.Name;
                }
            }
            else if (clientIdList != null && clientIdList.Count > 1)
            {
                clientName = "Multiplos_Clientes";
            }

            // Gerar PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    // Header
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text("Relatório de Horas")
                                .FontSize(20)
                                .Bold()
                                .FontColor(Colors.Grey.Darken4);
                            
                            column.Item().Text($"Período: {minDate?.ToString("dd/MM/yyyy")} a {maxDate?.ToString("dd/MM/yyyy")}")
                                .FontSize(10)
                                .FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(120).AlignRight().Column(column =>
                        {
                            column.Item().Background(Colors.Purple.Medium)
                                .Padding(10)
                                .AlignCenter()
                                .Text(text =>
                                {
                                    text.Span("Total: ").FontColor(Colors.White).FontSize(10);
                                    text.Span(totalFormatted).FontColor(Colors.White).FontSize(16).Bold();
                                });
                        });
                    });

                    // Content
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        // Definir colunas
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);  // Data
                            columns.RelativeColumn(2);   // Usuário
                            columns.RelativeColumn(1.5f); // Área
                            columns.ConstantColumn(50);  // Horas
                            columns.RelativeColumn(2);   // Cliente
                            columns.RelativeColumn(2);   // Solicitante
                            columns.RelativeColumn(1.5f); // Tipo
                        });

                        // Header da tabela
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Data").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Usuário").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Área").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Horas").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Cliente").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Solicitante").FontColor(Colors.White).Bold();
                            header.Cell().Background(Colors.Purple.Medium).Padding(5).Text("Tipo").FontColor(Colors.White).Bold();
                        });

                        // Dados
                        int rowIndex = 0;
                        foreach (var item in filteredRecords)
                        {
                            var bgColor = rowIndex % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                            
                            table.Cell().Background(bgColor).Padding(5).Text(item.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Background(bgColor).Padding(5).Text(item.Attorney.Name);
                            table.Cell().Background(bgColor).Padding(5).Text(item.Department.Name);
                            table.Cell().Background(bgColor).Padding(5).Text(item.CalculoHoras());
                            table.Cell().Background(bgColor).Padding(5).Text(item.Client.Name);
                            table.Cell().Background(bgColor).Padding(5).Text(item.Solicitante);
                            table.Cell().Background(bgColor).Padding(5).Text(item.RecordType.ToString());
                            
                            rowIndex++;
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });

            // Gerar o PDF em memória
            var pdfBytes = document.GeneratePdf();
            
            string fileName = $"Relatorio_Horas_{clientName}_{DateTime.Now:yyyyMMdd}.pdf";
            
            return File(pdfBytes, "application/pdf", fileName);
        }

        private PictureType GetPictureType(string mimeType)
        {
            switch (mimeType)
            {
                case "image/png":
                    return PictureType.PNG;
                case "image/jpeg":
                    return PictureType.JPEG;
                default:
                    return PictureType.PNG;
            }
        }

        private byte[] InjectGradientFill(byte[] xlsxBytes)
        {
            var ms = new MemoryStream();
            ms.Write(xlsxBytes, 0, xlsxBytes.Length);
            ms.Position = 0;

            using (var spreadDoc = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ms, true))
            {
                var wbPart = spreadDoc.WorkbookPart;
                var wsPart = wbPart.WorksheetParts.First();
                var ws = wsPart.Worksheet;

                var sheetData = ws.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                var row1 = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>()
                    .FirstOrDefault(r => r.RowIndex == 1);
                if (row1 == null) return xlsxBytes;

                var cellA1 = row1.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>()
                    .FirstOrDefault(c => c.CellReference == "A1");
                if (cellA1 == null) return xlsxBytes;

                var stylesPart = wbPart.WorkbookStylesPart;
                var stylesheet = stylesPart.Stylesheet;

                var gradientFill = new DocumentFormat.OpenXml.Spreadsheet.GradientFill
                {
                    Type = DocumentFormat.OpenXml.Spreadsheet.GradientValues.Linear,
                    Degree = 90  // 90° = cima para baixo
                };
                gradientFill.Append(new DocumentFormat.OpenXml.Spreadsheet.GradientStop
                {
                    Position = 0,
                    Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FFFFFFFF" }
                });
                gradientFill.Append(new DocumentFormat.OpenXml.Spreadsheet.GradientStop
                {
                    Position = 1,
                    Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = "FF9DC3E6" }
                });

                var newFill = new DocumentFormat.OpenXml.Spreadsheet.Fill();
                newFill.Append(gradientFill);

                stylesheet.Fills.Append(newFill);
                uint newFillId = (uint)(stylesheet.Fills.Count() - 1);

                uint currentXfId = cellA1.StyleIndex?.Value ?? 0;
                var cellXfs = stylesheet.CellFormats;
                var currentXf = (DocumentFormat.OpenXml.Spreadsheet.CellFormat)cellXfs.ElementAt((int)currentXfId).Clone();
                currentXf.FillId = newFillId;
                currentXf.ApplyFill = true;
                cellXfs.Append(currentXf);
                uint newXfId = (uint)(cellXfs.Count() - 1);

                cellA1.StyleIndex = newXfId;

                stylesheet.Save();
            } // spreadDoc disposed aqui, flush para ms

            return ms.ToArray();
        }


        // Ação para gerar Pré-Fatura em PDF
        public async Task<IActionResult> PreFatura(DateTime? minDate, DateTime? maxDate, string clientIds, int? attorneyId, int? departmentId, string recordType = null)
        {
            SetDefaultDateValues(ref minDate, ref maxDate);

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);

            List<int> clientIdList = null;
            if (!string.IsNullOrEmpty(clientIds))
            {
                clientIdList = clientIds.Split(',')
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }

            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientIdList, attorneyId, departmentId, recordTypeEnum);

            // Buscar logo da empresa
            byte[] logoData = null;
            string logoMimeType = null;
            try
            {
                var (imgData, mimeType, _, _) = await _parametroService.GetLogoAsync();
                logoData = imgData;
                logoMimeType = mimeType;
            }
            catch { /* sem logo configurado */ }

            // Agrupar por cliente
            var groupedByClient = filteredRecords
                .GroupBy(r => r.Client)
                .OrderBy(g => g.Key.Name)
                .ToList();

            // Calcular total geral de horas e valor
            TimeSpan totalGeralHoras = TimeSpan.Zero;
            double totalGeralValor = 0;

            // Pré-calcular horas e valores por grupo
            var clientData = new List<(Client client, List<ProcessRecord> records, TimeSpan totalHoras, double totalValor)>();
            foreach (var group in groupedByClient)
            {
                TimeSpan horasCliente = TimeSpan.Zero;
                double valorCliente = 0;
                foreach (var rec in group)
                {
                    var duracao = rec.CalculoHorasTotal();
                    horasCliente += duracao;
                    var valorReg = await _valorClienteService.GetValorForClienteAndUserAsync(rec.ClientId, rec.AttorneyId);
                    if (valorReg != null)
                        valorCliente += valorReg.Valor * duracao.TotalHours;
                }
                totalGeralHoras += horasCliente;
                totalGeralValor += valorCliente;
                clientData.Add((group.Key, group.ToList(), horasCliente, valorCliente));
            }

            string totalHorasFormatted = $"{(int)totalGeralHoras.TotalHours}:{totalGeralHoras.Minutes:00}h";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                    // Header
                    page.Header().PaddingBottom(10).Row(headerRow =>
                    {
                        // Logo da empresa (esquerda)
                        headerRow.RelativeItem().Column(col =>
                        {
                            if (logoData != null)
                            {
                                col.Item().MaxHeight(50).MaxWidth(120)
                                    .Image(logoData);
                            }
                        });

                        // Total geral (direita)
                        headerRow.ConstantItem(130).Background("#764ba2").Padding(10).Column(col =>
                        {
                            col.Item().Text("TOTAL GERAL").FontColor(Colors.White).FontSize(8).Bold().AlignCenter();
                            col.Item().Text($"R$ {totalGeralValor:N2}").FontColor(Colors.White).FontSize(16).Bold().AlignCenter();
                            col.Item().Text(totalHorasFormatted).FontColor(Colors.White).FontSize(9).AlignCenter();
                        });
                    });

                    page.Content().Column(content =>
                    {
                        // Título
                        content.Item().PaddingBottom(4).Text("Pré-Fatura de Honorários")
                            .FontSize(16).Bold().FontColor("#667eea");

                        content.Item().Text($"Período: {minDate?.ToString("dd/MM/yyyy")} a {maxDate?.ToString("dd/MM/yyyy")}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        content.Item().PaddingBottom(8).Text($"Emissão: {DateTime.Now:dd/MM/yyyy}")
                            .FontSize(9).FontColor(Colors.Grey.Medium);

                        content.Item().PaddingBottom(12).LineHorizontal(1).LineColor("#667eea");

                        // Blocos por cliente
                        foreach (var (client, records, horasCliente, valorCliente) in clientData)
                        {
                            string horasClienteFormatted = $"{(int)horasCliente.TotalHours}:{horasCliente.Minutes:00}h";

                            content.Item().PaddingBottom(12).Border(1).BorderColor(Colors.Grey.Lighten2).Column(clientBlock =>
                            {
                                // Cabeçalho do cliente
                                clientBlock.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(r =>
                                {
                                    r.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text(client.Name).FontSize(11).Bold();
                                        if (!string.IsNullOrEmpty(client.Document))
                                            c.Item().Text($"Doc: {client.Document}").FontSize(8).FontColor(Colors.Grey.Medium);
                                    });
                                    r.ConstantItem(160).AlignRight().Column(c =>
                                    {
                                        c.Item().Text($"Total horas: {horasClienteFormatted}").FontSize(9).AlignRight();
                                        c.Item().Text($"Valor: R$ {valorCliente:N2}").FontSize(10).Bold().FontColor("#667eea").AlignRight();
                                    });
                                });

                                // Tabela de registros
                                clientBlock.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.ConstantColumn(55);   // Data
                                        cols.RelativeColumn(1.5f); // Responsável
                                        cols.RelativeColumn(3);    // Descrição
                                        cols.ConstantColumn(40);   // Horas
                                        cols.ConstantColumn(60);   // Valor
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Background("#764ba2").Padding(4).Text("Data").FontColor(Colors.White).Bold().FontSize(8);
                                        h.Cell().Background("#764ba2").Padding(4).Text("Responsável").FontColor(Colors.White).Bold().FontSize(8);
                                        h.Cell().Background("#764ba2").Padding(4).Text("Descrição").FontColor(Colors.White).Bold().FontSize(8);
                                        h.Cell().Background("#764ba2").Padding(4).Text("Horas").FontColor(Colors.White).Bold().FontSize(8);
                                        h.Cell().Background("#764ba2").Padding(4).Text("Valor").FontColor(Colors.White).Bold().FontSize(8);
                                    });

                                    int idx = 0;
                                    foreach (var rec in records)
                                    {
                                        var bg = idx % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;
                                        var duracao = rec.CalculoHorasTotal();
                                        string horasRec = $"{(int)duracao.TotalHours}:{duracao.Minutes:00}";

                                        table.Cell().Background(bg).Padding(4).Text(rec.Date.ToString("dd/MM/yy")).FontSize(8);
                                        table.Cell().Background(bg).Padding(4).Text(rec.Attorney.Name).FontSize(8);
                                        table.Cell().Background(bg).Padding(4).Text(rec.Description).FontSize(8);
                                        table.Cell().Background(bg).Padding(4).Text(horasRec).FontSize(8);
                                        table.Cell().Background(bg).Padding(4).Text("—").FontSize(8).AlignCenter();
                                        idx++;
                                    }
                                });
                            });
                        }

                        // Rodapé do documento
                        content.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem().Text("* Documento sem valor fiscal. Sujeito a revisão.")
                                .FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                            r.ConstantItem(200).AlignRight().Text($"Total Geral: R$ {totalGeralValor:N2}")
                                .FontSize(11).Bold().FontColor("#667eea");
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        text.Span(" de ").FontSize(8).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            string fileName = $"PreFatura_{minDate:yyyyMMdd}_{maxDate:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
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

        private void PopulateViewData(DateTime? minDate, DateTime? maxDate, List<int> clientIds, int? attorneyId, string recordType)
        {
            ViewData["minDate"] = minDate.Value.ToString("yyyy-MM-dd");
            ViewData["maxDate"] = maxDate.Value.ToString("yyyy-MM-dd");
            ViewData["clientIds"] = clientIds != null && clientIds.Any() ? string.Join(",", clientIds) : null;
            ViewData["attorneyId"] = attorneyId;
            ViewData["selectedRecordType"] = recordType;
        }

        private async Task PopulateViewBag()
        {
            ViewBag.Clients = await _clientService.FindAllAsync();
            ViewBag.Attorneys = await _attorneyService.FindAllAsync();
            ViewBag.Departments = await _departmentService.FindAllAsync();

            Attorney usuario = _isessao.BuscarSessaoDoUsuario();
            ViewBag.LoggedUserId = usuario.Id;
            ViewBag.UserProfile = usuario.Perfil;
            ViewBag.CurrentUserPerfil = usuario.Perfil;
        }

        #endregion
    }
}

