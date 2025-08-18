using CommunityToolkit.Mvvm.Messaging.Messages;

namespace POSKU.Desktop;

public sealed class StockChangedMessage : ValueChangedMessage<int>
{
    // Value = ProductId
    public StockChangedMessage(int productId) : base(productId) { }
}
