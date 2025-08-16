namespace POSKU.Core;

public class Warehouse
{
    public int Id { get; set; }
    public string Code { get; set; } = "MAIN";
    public string Name { get; set; } = "Gudang Utama";
    public bool IsDefault { get; set; } = true;
}
