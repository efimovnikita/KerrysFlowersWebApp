using MongoDB.Driver;

namespace SharedLibrary.Providers;

public class VioletRepository : IVioletRepository
{
    private readonly IMongoDatabase _database;

    public VioletRepository(string connectionString, string dbDatabase)
    {
        MongoClient client = new MongoClient(connectionString);
        _database = client.GetDatabase(dbDatabase);
    }

    public Violet GetOrCreateViolet(Guid id, string name, string breeder, string description, List<string> tags,
        DateTime breedingDate, List<Image> images, bool isChimera, List<VioletColor> colors, VioletSize size)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");

        var filter = Builders<Violet>.Filter.Eq("Id", id);

        var violet = violetsCollection.Find(filter).FirstOrDefault();
        if (violet != null)
        {
            return violet;
        }

        violet = new Violet(id, name, breeder, description, tags, breedingDate, images, isChimera, colors, size);
        violetsCollection.InsertOne(violet);

        return violet;
    }

    public bool UpdateViolet(Violet updatedViolet)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq("Id", updatedViolet.Id);

        var updateDefinition = Builders<Violet>.Update
            .Set(v => v.Name, updatedViolet.Name)
            .Set(v => v.Breeder, updatedViolet.Breeder)
            .Set(v => v.Description, updatedViolet.Description)
            .Set(v => v.Tags, updatedViolet.Tags)
            .Set(v => v.Images, updatedViolet.Images)
            .Set(v => v.BreedingDate, updatedViolet.BreedingDate)
            .Set(v => v.IsChimera, updatedViolet.IsChimera)
            .Set(v => v.Colors, updatedViolet.Colors)
            .Set(v => v.Size, updatedViolet.Size)
            .CurrentDate(v => v.PublishDate); // Assuming we want to update the publish date as the current date

        var updateResult = violetsCollection.UpdateOne(filter, updateDefinition);

        return updateResult.ModifiedCount > 0;
    }

    public List<Violet> GetAllViolets()
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        return violetsCollection.Find(Builders<Violet>.Filter.Empty).ToList();
    }

    public Violet GetVioletById(Guid id)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq("Id", id);
        return violetsCollection.Find(filter).FirstOrDefault();
    }

    public bool DeleteViolet(Guid id)
    {
        var violetsCollection = _database.GetCollection<Violet>("Violets");
        var filter = Builders<Violet>.Filter.Eq(v => v.Id, id);

        var deleteResult = violetsCollection.DeleteOne(filter);

        return deleteResult.DeletedCount > 0;
    }
}