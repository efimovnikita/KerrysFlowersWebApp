﻿using SharedLibrary;

namespace KFTelegramBot.Providers;

public interface IVioletRepository
{
    Violet GetOrCreateViolet(Guid id, string name, string breeder, string description, List<string> tags,
        DateTime breedingDate, List<Image> images, bool isChimera, List<VioletColor> colors);
    bool UpdateViolet(Violet updatedViolet);
    List<Violet> GetAllViolets();
    Violet GetVioletById(Guid id);
    bool DeleteViolet(Guid id);
}