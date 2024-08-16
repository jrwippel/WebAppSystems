using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Org.BouncyCastle.Asn1.Pkcs;
using System.Globalization;
using System.Text;
using WebAppSystems.Filters;
using WebAppSystems.Models;
using WebAppSystems.Models.Enums;
using WebAppSystems.Models.ViewModels;
using WebAppSystems.Services;
using WebAppSystems.Models.Enums;
using NPOI.SS.Util;
using Microsoft.AspNetCore.Http;


namespace WebAppSystems.Controllers
{
    [PaginaParaUsuarioLogado]
    [PaginaRestritaSomenteAdmin]
    public class MensalistaController : Controller
    {


        private readonly ProcessRecordService _processRecordService;

        private readonly ClientService _clientService;

        private readonly AttorneyService _attorneyService;        

        private readonly DepartmentService _departmentService;

        private readonly IWebHostEnvironment _env;

        private readonly MensalistaService _mensalistaService;
        private ICellStyle lightGrayStyle;
        private ICellStyle veryLightGrayStyle;

        public MensalistaController(ProcessRecordService processRecordService, ClientService clientService, AttorneyService attorneyService, IWebHostEnvironment env,
            DepartmentService departmentService, MensalistaService mensalistaService)
        {
            _processRecordService = processRecordService;
            _clientService = clientService;
            _attorneyService = attorneyService;            
            _env = env;
            _departmentService = departmentService;
            _mensalistaService = mensalistaService;

        }

        public async Task<IActionResult> Index()
        {
            await PopulateViewBag();
            return View();
        }

        public async Task<IActionResult> SimpleSearch(string monthYearString, int? clientId, int? departmentId)
        {
            DateTime? monthYear = null;

            if (!string.IsNullOrEmpty(monthYearString) && monthYearString.Length == 6)
            {
                int month = int.Parse(monthYearString.Substring(0, 2));
                int year = int.Parse(monthYearString.Substring(2, 4));

                monthYear = new DateTime(year, month, 1);
            }

            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            PopulateViewData(monthYear.Value, clientId, departmentId);
            await PopulateViewBag();

            var result = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            // Ordena os resultados pelo valor líquido antes de criar a planilha
            result.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });

            ViewData["inputMonthYear"] = monthYearString; // Adicione esta linha

            return View(result);
        }


        #region Private Helpers

        private void ConvertMonthYearToRange(DateTime monthYear, out DateTime minDate, out DateTime maxDate)
        {
            minDate = new DateTime(monthYear.Year, monthYear.Month, 1);
            maxDate = minDate.AddMonths(1).AddDays(-1);
        }


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

        private void PopulateViewData(DateTime monthYear, int? clientId, int? departmentId)
        {
            ViewData["monthYear"] = monthYear.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;
        }


        private async Task PopulateViewBag()
        {
            ViewBag.Clients = await _clientService.FindAllAsync();
            ViewBag.Attorneys = await _attorneyService.FindAllAsync();
            ViewBag.Department = await _departmentService.FindAllAsync();
        }

        public async Task<IActionResult> ResultadoMes(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros

            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Monthly);

            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;
            ViewData["inputMonthYear"] = monthYear.Value.ToString("MM/yyyy");

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }


        public async Task<IActionResult> ResultadoMedia(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas dos últimos três meses
            DateTime startOfSelectedMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
            DateTime endOfSelectedMonth = startOfSelectedMonth.AddMonths(1).AddDays(-1);
            DateTime startOfThreeMonthsAgo = startOfSelectedMonth.AddMonths(-3);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros
            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(startOfThreeMonthsAgo, endOfSelectedMonth, clientId, departmentId, QueryType.Average);


            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }
        
        public async Task<IActionResult> ResultadoAcumulado(int id, DateTime? monthYear, int? clientId, int? departmentId)
        {
            var mensalista = await _mensalistaService.FindByIdAsync(id);
            if (mensalista == null)
            {
                return NotFound();
            }

            // Se monthYear não tiver valor, definimos para a data atual
            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            // Convertendo monthYear para o intervalo de datas dos últimos três meses
            DateTime startOfSelectedMonth = new DateTime(monthYear.Value.Year, monthYear.Value.Month, 1);
            DateTime endOfSelectedMonth = startOfSelectedMonth.AddMonths(1).AddDays(-1);
            DateTime startOfThreeMonthsAgo = startOfSelectedMonth.AddMonths(-3);

            // Obtendo as informações de MensalistaHoursViewModel usando os parâmetros

            var mensalistaHours = await _processRecordService.FindByDateMensalistaAsync(startOfThreeMonthsAgo, endOfSelectedMonth, clientId, departmentId, QueryType.Cumulative);

            var specificMensalistaHours = mensalistaHours.FirstOrDefault(m => m.Mensalista.Id == id);

            if (specificMensalistaHours == null)
            {
                return NotFound();
            }            

            // Armazenar os parâmetros no ViewData
            ViewData["monthYear"] = monthYear.Value.ToString("yyyy-MM");
            ViewData["clientId"] = clientId;
            ViewData["departmentId"] = departmentId;

            return View(new List<MensalistaHoursViewModel> { specificMensalistaHours });
        }    

        



        public async Task<IActionResult> DownloadReport(string monthYearString, int? clientId, int? departmentId, string recordType = null, string format = "xlsx")
        {
            DateTime? monthYear = null;

            if (!string.IsNullOrEmpty(monthYearString) && monthYearString.Length == 6)
            {
                int month = int.Parse(monthYearString.Substring(0, 2));
                int year = int.Parse(monthYearString.Substring(2, 4));

                monthYear = new DateTime(year, month, 1);
            }

            if (!monthYear.HasValue)
            {
                monthYear = DateTime.Now;
            }

            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            RecordType? recordTypeEnum = null;
            if (!string.IsNullOrEmpty(recordType))
            {
                recordTypeEnum = Enum.Parse<RecordType>(recordType, true);
            }

            var filteredRecords = await _processRecordService.FindByDateAsync(minDate, maxDate, clientId, departmentId, recordTypeEnum);

            if (format != "xlsx")
            {
                return BadRequest("Formato inválido");
            }

            var workbook = new XSSFWorkbook();
            // CreateMainSheet(workbook, filteredRecords, clientId);

        

            await CreateMensalidadeSheet(workbook, clientId, departmentId);
            await CreateResultadoMesSheet(workbook, clientId, departmentId, monthYear);
            await CreateMediaMesesSheet(workbook, clientId, departmentId, monthYear);
            await CreateAcumuladoMesesSheet(workbook, clientId, departmentId, monthYear);           

            string fileName = await GenerateFileName(clientId);
            return ConvertWorkbookToFile(workbook, fileName);
        }




        private async Task CreateMensalidadeSheet(XSSFWorkbook workbook, int? clientId, int? departmentId)
        {
            DateTime? monthYear = DateTime.Now;
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });

            var sheet = workbook.CreateSheet("Mensalidades");

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }

            var headerRow = sheet.CreateRow(0);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("VALOR MENSAL BRUTO");
            headerRow.CreateCell(2).SetCellValue("TRIBUTOS");
            headerRow.CreateCell(3).SetCellValue("COMISSÃO PARCEIRO");
            headerRow.CreateCell(4).SetCellValue("COMISSÃO SÓCIO");
            headerRow.CreateCell(5).SetCellValue("VALOR MENSAL LÍQUIDO");
            headerRow.CreateCell(6).SetCellValue(departmentName);
            headerRow.CreateCell(7).SetCellValue("VALOR DA ÁREA BRUTO");
            headerRow.CreateCell(8).SetCellValue("VALOR MENSAL LÍQUIDO");

            for (int j = 0; j < 9; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho
            sheet.SetAutoFilter(new CellRangeAddress(0, 0, 0, 8));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 1);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 9; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                row.GetCell(1).SetCellValue((double)item.Mensalista.ValorMensalBruto);
                row.GetCell(2).SetCellValue((double)item.Tributos);
                row.GetCell(3).SetCellValue((double)item.Mensalista.ComissaoParceiro);
                row.GetCell(4).SetCellValue((double)item.Mensalista.ComissaoSocio);
                row.GetCell(5).SetCellValue((double)item.ValorMensalLiquido);
                row.GetCell(6).SetCellValue($"{item.Percentual:0.00}");
                row.GetCell(7).SetCellValue((double)item.ValorAreaBruto);
                row.GetCell(8).SetCellValue((double)item.ValorAreaLiquido);
            }


            for (int colIndex = 0; colIndex < 9; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

        private async Task CreateResultadoMesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)

        {            
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId);
            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });
            var sheet = workbook.CreateSheet("Resultado do Mês");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }
            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 }); // RGB para cinza escuro

            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center; // Centralizar o texto

            // Estilizando a fonte
            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index; // Fonte branca
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            // Inserir a primeira linha com o nome do departamento
            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;


            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }


            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            if (monthYear.HasValue)
            {
                string monthName = monthYear.Value.ToString("MMM", new CultureInfo("pt-BR")).TrimEnd('.').ToLower();
                headerRow.CreateCell(1).SetCellValue($"{monthName}/{monthYear.Value:yy}");

            }
            else
            {
                headerRow.CreateCell(1).SetCellValue("MÊS");
            }

            headerRow.CreateCell(2).SetCellValue("HORA TÉCNICA BRUTA");
            headerRow.CreateCell(3).SetCellValue("HORA TÉCNICA LÍQUIDA");
            headerRow.CreateCell(4).SetCellValue("RESULTADO BRUTO");
            headerRow.CreateCell(5).SetCellValue("RESULTADO LÍQUIDO");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                  

                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");                
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }



            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

   

        private async Task CreateMediaMesesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)

        {
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Average);

            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });
            var sheet = workbook.CreateSheet("Média 3 meses");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";  // Caso departmentName seja nulo ou vazio, use "%" como padrão.
            }
            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 }); // RGB para cinza escuro

            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center; // Centralizar o texto

            // Estilizando a fonte
            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index; // Fonte branca
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            // Inserir a primeira linha com o nome do departamento
            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;


            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;

            // Modificando lightGrayStyle para também ter o formato de número:
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");


            // Define bordas brancas para os estilos
            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }


            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("Média últimos 3 meses");
            headerRow.CreateCell(2).SetCellValue("Média bruta ú. 3 mês");
            headerRow.CreateCell(3).SetCellValue("Média Líquida últimos 3 meses");
            headerRow.CreateCell(4).SetCellValue("Bruto últimos 3 meses");
            headerRow.CreateCell(5).SetCellValue("Líquido últimos 3 meses");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }



            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }

        private async Task CreateAcumuladoMesesSheet(XSSFWorkbook workbook, int? clientId, int? departmentId, DateTime? monthYear)
        {
            ConvertMonthYearToRange(monthYear.Value, out DateTime minDate, out DateTime maxDate);

            var results = await _processRecordService.FindByDateMensalistaAsync(minDate, maxDate, clientId, departmentId, QueryType.Cumulative);

            // Ordena os resultados pelo valor líquido antes de criar a planilha
            results.Sort((a, b) =>
            {
                return a.ValorResultadoLiquido.CompareTo(b.ValorResultadoLiquido);
            });

            var sheet = workbook.CreateSheet("Acumulado 3 meses");

            var departmentName = await _departmentService.GetDepartmentNameByIdAsync(departmentId);
            if (string.IsNullOrEmpty(departmentName))
            {
                departmentName = "%";
            }

            var titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            var darkGreyColor = new XSSFColor(new byte[] { 169, 169, 169 });
            titleStyle.SetFillForegroundColor(darkGreyColor);
            titleStyle.FillPattern = FillPattern.SolidForeground;
            titleStyle.Alignment = HorizontalAlignment.Center;

            var titleFont = workbook.CreateFont();
            titleFont.Color = HSSFColor.White.Index;
            titleFont.IsBold = true;
            titleStyle.SetFont(titleFont);

            var titleRow = sheet.CreateRow(0);
            var titleCell = titleRow.CreateCell(0);
            titleCell.SetCellValue($"CLIENTES {departmentName.ToUpper()}");
            titleCell.CellStyle = titleStyle;

            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 5));

            var numberStyle = workbook.CreateCellStyle();
            numberStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            var headerStyle = workbook.CreateCellStyle();
            headerStyle.FillForegroundColor = HSSFColor.Grey40Percent.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;

            var lightGrayStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            lightGrayStyle.SetFillForegroundColor(new XSSFColor(new byte[] { 230, 230, 230 }));
            lightGrayStyle.FillPattern = FillPattern.SolidForeground;
            lightGrayStyle.DataFormat = workbook.CreateDataFormat().GetFormat("#,##0.00");

            short borderColor = IndexedColors.White.Index;

            var styles = new[] { headerStyle, lightGrayStyle, numberStyle };
            foreach (var style in styles)
            {
                style.BorderTop = BorderStyle.Thin;
                style.TopBorderColor = borderColor;
                style.BorderRight = BorderStyle.Thin;
                style.RightBorderColor = borderColor;
                style.BorderBottom = BorderStyle.Thin;
                style.BottomBorderColor = borderColor;
                style.BorderLeft = BorderStyle.Thin;
                style.LeftBorderColor = borderColor;
            }

            var headerRow = sheet.CreateRow(1);
            headerRow.CreateCell(0).SetCellValue("NOME");
            headerRow.CreateCell(1).SetCellValue("Acumulado últimos 3 meses");
            headerRow.CreateCell(2).SetCellValue("Bruto últimos 3 meses");
            headerRow.CreateCell(3).SetCellValue("Líquido últimos 3 meses");
            headerRow.CreateCell(4).SetCellValue("Resultado Bruto últimos 3 meses");
            headerRow.CreateCell(5).SetCellValue("Líquido últimos 3 meses");
            for (int j = 0; j < 6; j++)
            {
                headerRow.GetCell(j).CellStyle = headerStyle;
            }
            // Configurar filtro nas células do cabeçalho para todas as colunas
            sheet.SetAutoFilter(new CellRangeAddress(1, 1, 0, 5));

            for (int i = 0; i < results.Count; i++)
            {
                var item = results[i];
                var row = sheet.CreateRow(i + 2);

                ICellStyle currentStyle = (i % 2 == 0) ? lightGrayStyle : numberStyle;

                for (int col = 0; col < 6; col++)
                {
                    var cell = row.CreateCell(col);
                    cell.CellStyle = currentStyle;
                }

                row.GetCell(0).SetCellValue(item.Mensalista.Client.Name);
                double totalHours = Math.Floor(item.TotalHours);
                double totalMinutes = (item.TotalHours - totalHours) * 60;  

                row.GetCell(1).SetCellValue($"{totalHours}:{Math.Round(totalMinutes)}");
                //row.GetCell(2).SetCellValue((double)item.ValorTotalHoras);

                double valotTotalHoras = Math.Round((double)item.ValorTotalHoras, 2);
                row.GetCell(2).SetCellValue(valotTotalHoras);

                //row.GetCell(3).SetCellValue(Math.Round((double)item.ValorHoraTecLiquida, 2));

                double valorHoraTecnicaLiquida = Math.Round((double)item.ValorHoraTecLiquida, 2);
                row.GetCell(3).SetCellValue(valorHoraTecnicaLiquida);

                //row.GetCell(4).SetCellValue((double)item.ValorResultadoBruto);

                double valorResultadoBruto = Math.Round((double)item.ValorResultadoBruto, 2);
                row.GetCell(4).SetCellValue(valorResultadoBruto);


                double valorResultadoLiquido = Math.Round((double)item.ValorResultadoLiquido, 2);
                row.GetCell(5).SetCellValue(valorResultadoLiquido);

                // Aplicar formatação condicional para todas as colunas de valor
                for (int col = 1; col <= 5; col++)
                {
                    double cellValue;
                    var cell = row.GetCell(col);
                    if (cell != null && cell.CellType == CellType.Numeric)
                    {
                        cellValue = cell.NumericCellValue;
                        IFont font = workbook.CreateFont();

                        if (cellValue < 0)
                        {
                            font.Color = HSSFColor.Red.Index; // Fonte vermelha
                        }
                        else if (cellValue > 0)
                        {
                            font.Color = HSSFColor.Green.Index; // Fonte verde
                        }

                        ICellStyle conditionalStyle = workbook.CreateCellStyle();
                        conditionalStyle.CloneStyleFrom(currentStyle);
                        conditionalStyle.SetFont(font);
                        cell.CellStyle = conditionalStyle;
                    }
                }

            }

            for (int colIndex = 0; colIndex < 6; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
            }
        }



        private async Task<string> GenerateFileName(int? clientId)
        {
            string clientName = null;
            if (clientId.HasValue)
            {
                var client = await _clientService.FindByIdAsync(clientId.Value);
                if (client != null)
                {
                    clientName = client.Name;
                }
            }

            string fileName = "Relatório_TimeSheet";
            if (!string.IsNullOrEmpty(clientName))
            {
                fileName += $"_{clientName}";
            }
            fileName += ".xlsx";
            return fileName;
        }


        private IActionResult ConvertWorkbookToFile(XSSFWorkbook workbook, string fileName)
        {
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



        #endregion

    }
}
