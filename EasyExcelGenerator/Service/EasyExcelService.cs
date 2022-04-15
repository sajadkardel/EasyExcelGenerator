﻿using ClosedXML.Excel;
using ClosedXML.Report.Utils;
using EasyExcelGenerator.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EasyExcelGenerator.Service;

// TODO: Remove static and make them work with DI
public static class EasyExcelService
{
    /// <summary>
    /// Generate Excel file into file result
    /// </summary>
    /// <param name="easyExcelBuilder"></param>
    /// <returns></returns>
    public static GeneratedExcelFile GenerateExcel(EasyExcelBuilder easyExcelBuilder)
    {
        using var xlWorkbook = ClosedXmlEngine(easyExcelBuilder);

        // Save
        using var stream = new MemoryStream();

        xlWorkbook.SaveAs(stream);

        var content = stream.ToArray();

        return new GeneratedExcelFile { Content = content };
    }

    /// <summary>
    /// Generate Excel file and save it in path and return the saved url
    /// </summary>
    /// <param name="easyExcelBuilderFile"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public static string GenerateExcel(EasyExcelBuilder easyExcelBuilderFile, string savePath)
    {
        using var xlWorkbook = ClosedXmlEngine(easyExcelBuilderFile);

        var saveUrl = $"{savePath}\\{easyExcelBuilderFile.FileName}.xlsx";

        // Save
        xlWorkbook.SaveAs(saveUrl);

        return saveUrl;
    }

    /// <summary>
    /// Generate Simple Grid Excel file from special model configured options with EasyExcel attributes
    /// </summary>
    /// <param name="easyGridExcelBuilder"></param>
    /// <returns></returns>
    public static GeneratedExcelFile GenerateGridExcel(EasyGridExcelBuilder easyGridExcelBuilder)
    {
        var easyExcelBuilder = easyGridExcelBuilder.ConvertEasyGridExcelBuilderToEasyExcelBuilder();

        return GenerateExcel(easyExcelBuilder);
    }

    /// <summary>
    /// Generate Simple Grid Excel file from special model configured options with EasyExcel attributes
    /// Save it in path and return the saved url
    /// </summary>
    /// <param name="easyGridExcelBuilder"></param>
    /// <param name="savePath"></param>
    /// <returns></returns>
    public static string GenerateGridExcel(EasyGridExcelBuilder easyGridExcelBuilder, string savePath)
    {
        var easyExcelBuilder = easyGridExcelBuilder.ConvertEasyGridExcelBuilderToEasyExcelBuilder();

        return GenerateExcel(easyExcelBuilder, savePath);
    }

    private static XLWorkbook ClosedXmlEngine(EasyExcelBuilder easyExcelBuilder)
    {
        if (easyExcelBuilder.FileName.IsNullOrWhiteSpace())
            easyExcelBuilder.FileName = $"EasyExcelGeneratedFile_{DateTime.Now:yyyy-MM-dd HH-mm-ss}";

        //-------------------------------------------
        //  Create Workbook (integrated with using statement)
        //-------------------------------------------
        var xlWorkbook = new XLWorkbook
        {
            RightToLeft = easyExcelBuilder.AllSheetsDefaultStyle.AllSheetsDefaultDirection == SheetDirection.RightToLeft,
            ColumnWidth = easyExcelBuilder.AllSheetsDefaultStyle.AllSheetsDefaultColumnWidth,
            RowHeight = easyExcelBuilder.AllSheetsDefaultStyle.AllSheetsDefaultRowHeight
        };

        // Check any sheet available
        if (easyExcelBuilder.Sheets.Count == 0)
            throw new Exception("No sheet is available to create Excel workbook");

        // Check sheet names are unique
        var sheetNames = easyExcelBuilder.Sheets
            .Where(s => s.SheetName.IsNullOrWhiteSpace() is false)
            .Select(s => s.SheetName)
            .ToList();

        var uniqueSheetNames = sheetNames.Distinct().ToList();

        if (sheetNames.Count != uniqueSheetNames.Count)
            throw new Exception("Sheet names should be unique");

        // Auto naming for sheets

        int i = 1;
        foreach (Sheet sheet in easyExcelBuilder.Sheets)
        {
            if (sheet.SheetName.IsNullOrWhiteSpace())
            {
                var isNameOk = false;

                while (isNameOk is false)
                {
                    var possibleName = $"Sheet{i}";

                    isNameOk = easyExcelBuilder.Sheets.Any(s => s.SheetName == possibleName) is false;

                    if (isNameOk)
                        sheet.SheetName = possibleName;

                    i++;
                }
            }
        }

        //-------------------------------------------
        //  Add Sheets one by one to ClosedXML Workbook instance
        //-------------------------------------------
        foreach (var sheet in easyExcelBuilder.Sheets)
        {
            // Set name
            var xlSheet = xlWorkbook.Worksheets.Add(sheet.SheetName);

            // Set protection level
            var protection = xlSheet.Protect(sheet.SheetProtectionLevels.Password);
            if (sheet.SheetProtectionLevels.DeleteColumns)
                protection.Protect().AllowedElements = XLSheetProtectionElements.DeleteColumns;
            if (sheet.SheetProtectionLevels.EditObjects)
                protection.Protect().AllowedElements = XLSheetProtectionElements.EditObjects;
            if (sheet.SheetProtectionLevels.FormatCells)
                protection.Protect().AllowedElements = XLSheetProtectionElements.FormatCells;
            if (sheet.SheetProtectionLevels.FormatColumns)
                protection.Protect().AllowedElements = XLSheetProtectionElements.FormatColumns;
            if (sheet.SheetProtectionLevels.FormatRows)
                protection.Protect().AllowedElements = XLSheetProtectionElements.FormatRows;
            if (sheet.SheetProtectionLevels.InsertColumns)
                protection.Protect().AllowedElements = XLSheetProtectionElements.InsertColumns;
            if (sheet.SheetProtectionLevels.InsertHyperLinks)
                protection.Protect().AllowedElements = XLSheetProtectionElements.InsertHyperlinks;
            if (sheet.SheetProtectionLevels.InsertRows)
                protection.Protect().AllowedElements = XLSheetProtectionElements.InsertRows;
            if (sheet.SheetProtectionLevels.SelectLockedCells)
                protection.Protect().AllowedElements = XLSheetProtectionElements.SelectLockedCells;
            if (sheet.SheetProtectionLevels.DeleteRows)
                protection.Protect().AllowedElements = XLSheetProtectionElements.DeleteRows;
            if (sheet.SheetProtectionLevels.EditScenarios)
                protection.Protect().AllowedElements = XLSheetProtectionElements.EditScenarios;
            if (sheet.SheetProtectionLevels.SelectUnlockedCells)
                protection.Protect().AllowedElements = XLSheetProtectionElements.SelectUnlockedCells;
            if (sheet.SheetProtectionLevels.Sort)
                protection.Protect().AllowedElements = XLSheetProtectionElements.Sort;
            if (sheet.SheetProtectionLevels.UseAutoFilter)
                protection.Protect().AllowedElements = XLSheetProtectionElements.AutoFilter;
            if (sheet.SheetProtectionLevels.UsePivotTableReports)
                protection.Protect().AllowedElements = XLSheetProtectionElements.PivotTables;

            // Set direction
            if (sheet.SheetStyle.SheetDirection is not null)
                xlSheet.RightToLeft = sheet.SheetStyle.SheetDirection.Value == SheetDirection.RightToLeft;

            // Set default column width
            if (sheet.SheetStyle.SheetDefaultColumnWidth is not null)
                xlSheet.ColumnWidth = (double)sheet.SheetStyle.SheetDefaultColumnWidth;

            // Set default row height
            if (sheet.SheetStyle.SheetDefaultRowHeight is not null)
                xlSheet.RowHeight = (double)sheet.SheetStyle.SheetDefaultRowHeight;

            // Set visibility
            xlSheet.Visibility = sheet.SheetStyle.Visibility switch
            {
                SheetVisibility.Hidden => XLWorksheetVisibility.Hidden,
                SheetVisibility.VeryHidden => XLWorksheetVisibility.VeryHidden,
                _ => XLWorksheetVisibility.Visible
            };

            // Set TextAlign
            var textAlign = sheet.SheetStyle.SheetDefaultTextAlign ?? easyExcelBuilder.AllSheetsDefaultStyle.AllSheetsDefaultTextAlign;

            xlSheet.Columns().Style.Alignment.Horizontal = textAlign switch
            {
                TextAlign.Center => XLAlignmentHorizontalValues.Center,
                TextAlign.Right => XLAlignmentHorizontalValues.Right,
                TextAlign.Left => XLAlignmentHorizontalValues.Left,
                TextAlign.Justify => XLAlignmentHorizontalValues.Justify,
                _ => throw new ArgumentOutOfRangeException()
            };

            //-------------------------------------------
            //  Columns properties
            //-------------------------------------------
            foreach (var columnStyle in sheet.SheetColumnsStyle)
            {
                // Infer XLAlignment from "ColumnProp"
                var columnAlignmentHorizontalValue = columnStyle.ColumnTextAlign switch
                {
                    TextAlign.Center => XLAlignmentHorizontalValues.Center,
                    TextAlign.Justify => XLAlignmentHorizontalValues.Justify,
                    TextAlign.Left => XLAlignmentHorizontalValues.Left,
                    TextAlign.Right => XLAlignmentHorizontalValues.Right,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (columnStyle.ColumnWidth is not null)
                {
                    if (columnStyle.ColumnWidth.WidthCalculationType == ColumnWidthCalculationType.AdjustToContents)
                        xlSheet.Column(columnStyle.ColumnNo).AdjustToContents();

                    else
                        xlSheet.Column(columnStyle.ColumnNo).Width = (double)columnStyle.ColumnWidth.Width!;
                }

                if (columnStyle.AutoFit)
                    xlSheet.Column(columnStyle.ColumnNo).AdjustToContents();

                if (columnStyle.IsColumnHidden)
                    xlSheet.Column(columnStyle.ColumnNo).Hide();

                xlSheet.Column(columnStyle.ColumnNo).Style.Alignment
                    .SetHorizontal(columnAlignmentHorizontalValue);
            }

            //-------------------------------------------
            //  Map Tables
            //-------------------------------------------
            foreach (var table in sheet.SheetTables)
            {
                foreach (var tableRow in table.TableRows)
                {
                    xlSheet.ConfigureRow(tableRow, sheet.SheetColumnsStyle, sheet.IsSheetLocked ?? easyExcelBuilder.AreSheetsLockedByDefault);
                }

                var tableRange = xlSheet.Range(table.StartCellLocation.Y,
                    table.StartCellLocation.X,
                    table.EndLocation.Y,
                    table.EndLocation.X);

                // Config Outside-Border
                XLBorderStyleValues? outsideBorder = GetXlBorderLineStyle(table.OutsideBorder.BorderLineStyle);

                if (outsideBorder is not null)
                {
                    tableRange.Style.Border.SetOutsideBorder((XLBorderStyleValues)outsideBorder);
                    tableRange.Style.Border.SetOutsideBorderColor(XLColor.FromColor(table.OutsideBorder.BorderColor));
                }

                // Config Inside-Border
                XLBorderStyleValues? insideBorder = GetXlBorderLineStyle(table.InlineBorder.BorderLineStyle);

                if (insideBorder is not null)
                {
                    tableRange.Style.Border.SetInsideBorder((XLBorderStyleValues)insideBorder);
                    tableRange.Style.Border.SetInsideBorderColor(XLColor.FromColor(table.InlineBorder.BorderColor));
                }

                // Apply table merges here
                foreach (var mergedCells in table.MergedCells)
                {
                    xlSheet.Range(mergedCells).Merge();
                }
            }

            //-------------------------------------------
            //  Map Rows 
            //-------------------------------------------
            foreach (var sheetRow in sheet.SheetRows)
            {
                xlSheet.ConfigureRow(sheetRow, sheet.SheetColumnsStyle, sheet.IsSheetLocked ?? easyExcelBuilder.AreSheetsLockedByDefault);
            }

            //-------------------------------------------
            //  Map Cells
            //-------------------------------------------
            foreach (var cell in sheet.SheetCells)
            {
                if (cell.IsCellVisible is false)
                    continue;

                xlSheet.ConfigureCell(cell, sheet.SheetColumnsStyle, sheet.IsSheetLocked ?? easyExcelBuilder.AreSheetsLockedByDefault);
            }

            // Apply sheet merges here
            foreach (var mergedCells in sheet.MergedCells)
            {
                var rangeToMerge = xlSheet.Range(mergedCells).Cells();

                var value = rangeToMerge.FirstOrDefault(r => r.IsEmpty() is false)?.Value;

                rangeToMerge.First().SetValue(value);

                xlSheet.Range(mergedCells).Merge();
            }
        }

        return xlWorkbook;
    }

    private static EasyExcelBuilder ConvertEasyGridExcelBuilderToEasyExcelBuilder(this EasyGridExcelBuilder easyGridExcelBuilder)
    {
        var easyExcelBuilder = new EasyExcelBuilder();

        foreach (var gridExcelSheet in easyGridExcelBuilder.Sheets)
        {
            if (gridExcelSheet.DataList is IEnumerable records)
            {
                var headerRow = new Row();

                var dataRows = new List<Row>();

                // Get Header 

                bool headerCalculated = false;

                int yLocation = 1;

                string? sheetName = null;

                foreach (var record in records)
                {
                    var easyExcelSheetAttribute = record.GetType().GetCustomAttribute<EasyExcelGridSheetAttribute>();

                    sheetName = easyExcelSheetAttribute?.SheetName;

                    PropertyInfo[] properties = record.GetType().GetProperties();

                    int xLocation = 1;

                    var recordRow = new Row();

                    foreach (var prop in properties)
                    {
                        var easyExcelColumnAttribute = (EasyExcelGridColumnAttribute?)prop.GetCustomAttributes(true).FirstOrDefault(x => x is EasyExcelGridColumnAttribute);

                        // Header
                        if (headerCalculated is false)
                        {
                            headerRow.Cells.Add(new Cell(new CellLocation(xLocation, yLocation))
                            {
                                Value = easyExcelColumnAttribute?.HeaderName ?? prop.Name,
                                CellType = CellType.Text
                            });

                            headerRow.RowHeight = easyExcelSheetAttribute?.HeaderHeight;

                            headerRow.BackgroundColor = easyExcelSheetAttribute?.HeaderBackgroundColor ?? Color.Transparent;
                        }

                        // Data
                        recordRow.Cells.Add(new Cell(new CellLocation(xLocation, yLocation + 1))
                        {
                            Value = prop.GetValue(record),
                            CellType = easyExcelColumnAttribute?.ExcelDataType ?? CellType.Text
                        });

                        xLocation++;
                    }

                    dataRows.Add(recordRow);

                    yLocation++;

                    headerCalculated = true;
                }

                easyExcelBuilder.Sheets.Add(new Sheet
                {
                    SheetName = sheetName,

                    // Header Row
                    SheetRows = new List<Row> { headerRow },

                    // Table Data
                    SheetTables = new List<Table>
                    {
                        new()
                        {
                            TableRows = dataRows,
                            InlineBorder = new Border { BorderLineStyle = LineStyle.Thin },
                            OutsideBorder = new Border { BorderLineStyle = LineStyle.Thin }
                        }
                    }
                });
            }
            else
            {
                throw new Exception("GridExcelSheet object should be IEnumerable");
            }
        }

        return easyExcelBuilder;
    }

    private static void ConfigureCell(this IXLWorksheet xlSheet, Cell cell, List<ColumnStyle> columnProps, bool isSheetLocked)
    {
        // Infer XLDataType and value from "cell" CellType
        XLDataType? xlDataType;
        object cellValue = cell.Value;
        switch (cell.CellType)
        {
            case CellType.Number:
                xlDataType = XLDataType.Number;
                break;

            case CellType.Percentage:
                xlDataType = XLDataType.Text;
                cellValue = $"{cellValue}%";
                break;

            case CellType.Currency:
                xlDataType = XLDataType.Number;
                if (cellValue.IsNumber() is false)
                    throw new Exception("Cell with Currency CellType should be Number type");
                cellValue = Convert.ToDecimal(cellValue).ToString("##,###");
                break;

            case CellType.MiladiDate:
                xlDataType = XLDataType.DateTime;
                if (cellValue is not DateTime)
                    throw new Exception("Cell with MiladiDate CellType should be DateTime type");
                break;

            case CellType.Text:
            case CellType.Formula:
                xlDataType = XLDataType.Text;
                break;

            default: // = CellType.General
                xlDataType = null;
                break;
        }

        // Infer XLAlignment from "cell"
        XLAlignmentHorizontalValues? cellAlignmentHorizontalValue = cell.CellTextAlign switch
        {
            TextAlign.Center => XLAlignmentHorizontalValues.Center,
            TextAlign.Left => XLAlignmentHorizontalValues.Left,
            TextAlign.Right => XLAlignmentHorizontalValues.Right,
            TextAlign.Justify => XLAlignmentHorizontalValues.Justify,
            _ => null
        };

        // Get IsLocked property based on Sheet and Cell "IsLocked" prop
        bool? isLocked = cell.IsCellLocked;

        if (isLocked is null)
        { // Get from ColumnProps level
            var x = cell.CellLocation.X;

            var relatedColumnProp = columnProps.SingleOrDefault(c => c.ColumnNo == x);

            isLocked = relatedColumnProp?.IsColumnLocked;

            if (isLocked is null)
            { // Get from sheet level
                isLocked = isSheetLocked;
            }
        }

        //-------------------------------------------
        //  Map column per Cells loop cycle
        //-------------------------------------------
        var locationCell = xlSheet.Cell(cell.CellLocation.Y, cell.CellLocation.X);

        if (xlDataType is not null)
            locationCell.SetDataType((XLDataType)xlDataType);

        if (cell.CellType == CellType.Formula)
            locationCell.SetFormulaA1(cellValue.ToString());
        else
            locationCell.SetValue(cellValue);

        locationCell.Style
            .Alignment.SetWrapText(cell.Wordwrap);

        locationCell.Style.Protection.SetLocked((bool)isLocked!);

        if (cellAlignmentHorizontalValue is not null)
            locationCell.Style.Alignment.SetHorizontal((XLAlignmentHorizontalValues)cellAlignmentHorizontalValue!);
    }

    private static void ConfigureRow(this IXLWorksheet xlSheet, Row row, List<ColumnStyle> columnsStyleList, bool isSheetLocked)
    {
        foreach (var rowCell in row.Cells)
        {
            if (rowCell.IsCellVisible is false)
                continue;

            xlSheet.ConfigureCell(rowCell, columnsStyleList, isSheetLocked);
        }

        // Configure merged cells in the row
        foreach (var cellsToMerge in row.MergedCellsList)
        {
            // CellsToMerge example is "B2:D2"
            xlSheet.Range(cellsToMerge).Row(1).Merge();
        }

        if (row.Cells.Count != 0)
        {
            if (row.StartCellLocation is not null && row.EndCellLocation is not null)
            {
                var xlRow = xlSheet.Row(row.Cells.First().CellLocation.Y);
                if (row.RowHeight is not null)
                    xlRow.Height = (double)row.RowHeight;

                var xlRowRange = xlSheet.Range(row.StartCellLocation.Y, row.StartCellLocation.X, row.EndCellLocation.Y,
                    row.EndCellLocation.X);
                xlRowRange.Style.Font.SetFontColor(XLColor.FromColor(row.FontColor));
                xlRowRange.Style.Fill.SetBackgroundColor(XLColor.FromColor(row.BackgroundColor));

                XLBorderStyleValues? outsideBorder = GetXlBorderLineStyle(row.OutsideBorder.BorderLineStyle);

                if (outsideBorder is not null)
                {
                    xlRowRange.Style.Border.SetOutsideBorder((XLBorderStyleValues)outsideBorder);
                    xlRowRange.Style.Border.SetOutsideBorderColor(
                        XLColor.FromColor(row.OutsideBorder.BorderColor));
                }

                // TODO: For Inside border, the row should be considered as Ranged (like Table). I persume it is not important for this phase
            }
            else
            {
                var xlRow = xlSheet.Row(row.Cells.First().CellLocation.Y);
                if (row.RowHeight is not null)
                    xlRow.Height = (double)row.RowHeight;
                xlRow.Style.Font.SetFontColor(XLColor.FromColor(row.FontColor));
                xlRow.Style.Fill.SetBackgroundColor(XLColor.FromColor(row.BackgroundColor));
                xlRow.Style.Border.SetOutsideBorder(XLBorderStyleValues.Dotted);
                xlRow.Style.Border.SetInsideBorder(XLBorderStyleValues.Thick);
                xlRow.Style.Border.SetTopBorder(XLBorderStyleValues.Thick);
                xlRow.Style.Border.SetRightBorder(XLBorderStyleValues.DashDotDot);
            }
        }
    }

    private static XLBorderStyleValues? GetXlBorderLineStyle(LineStyle borderLineStyle)
    {
        return borderLineStyle switch
        {
            LineStyle.DashDotDot => XLBorderStyleValues.DashDotDot,
            LineStyle.Thick => XLBorderStyleValues.Thick,
            LineStyle.Thin => XLBorderStyleValues.Thin,
            LineStyle.Dotted => XLBorderStyleValues.Dotted,
            LineStyle.Double => XLBorderStyleValues.Double,
            LineStyle.DashDot => XLBorderStyleValues.DashDot,
            LineStyle.Dashed => XLBorderStyleValues.Dashed,
            LineStyle.SlantDashDot => XLBorderStyleValues.SlantDashDot,
            LineStyle.None => XLBorderStyleValues.None,
            _ => null
        };
    }

    private static bool IsNumber(this object value)
    {
        return value is sbyte
               || value is byte
               || value is short
               || value is ushort
               || value is int
               || value is uint
               || value is long
               || value is ulong
               || value is float
               || value is double
               || value is decimal;
    }
}