﻿namespace SharedLibrary.Providers;

public interface IVioletRepository
{
    Violet GetOrCreateViolet(Guid id, string name, string breeder, string description, List<string> tags,
        DateTime breedingDate, List<Image> images, bool isChimera, List<VioletColor> colors, VioletSize size);
    bool UpdateViolet(Violet updatedViolet);
    List<Violet> GetAllViolets();
    Violet GetVioletById(Guid id);
    bool DeleteViolet(Guid id);
    bool InsertWarehouseVioletItems(IEnumerable<WarehouseVioletItem> items);
    List<WarehouseVioletItem> GetAllWarehouseVioletItems();
    bool ClearAllWarehouseVioletItems();
    bool InsertOrder(Order order);
    bool SetOrderInactive(Guid orderId);
    List<Order> GetAllActiveOrders();
    bool DeleteAllOrders();
}