using NPOI.XSSF.UserModel;
using SharedLibrary;

namespace KFTelegramBot.Model;

public static class ExcelReader
{
    public static List<WarehouseVioletItem> ReadExcel(string filePath)
    {
        List<WarehouseVioletItem> items = [];

        try
        {
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var workbook = new XSSFWorkbook(file);
                var sheet = workbook.GetSheetAt(0); // Assuming that the data is in the first sheet.

                for (var row = 1; row <= sheet.LastRowNum; row++) // Skip header row (row 0)
                {
                    try
                    {
                        var currentRow = sheet.GetRow(row);
                
                        // Read cells from the current row and create a new WarehouseVioletItem object.
                        // Ensuring that there is a row and cells aren't null
                        if (currentRow == null) continue;
                        var stringGuidValue = currentRow.GetCell(0).StringCellValue;
                        var violetId = Guid.Parse(stringGuidValue);
                        var leafCount = (int)currentRow.GetCell(2).NumericCellValue;
                        var leafPrice = currentRow.GetCell(3).NumericCellValue;
                        var childCount = (int)currentRow.GetCell(4).NumericCellValue;
                        var childPrice = currentRow.GetCell(5).NumericCellValue;
                        var wholePlantCount = (int)currentRow.GetCell(6).NumericCellValue;
                        var wholePlantPrice = currentRow.GetCell(7).NumericCellValue;
                    
                        var item = new WarehouseVioletItem(violetId, leafCount, leafPrice, childCount, childPrice,
                            wholePlantCount, wholePlantPrice);
                        items.Add(item);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
        }
        catch (Exception)
        {
            return items;
        }

        return items;
    }
}