namespace ChickenPos.Web;
public sealed class ApiError(string message, int status=400):Exception(message) {public int Status {get;}=status;}
public record LoginInput(string Username,string Password);
public record ItemInput(int ProductId,int Quantity);
public record NewOrder(string OrderType,int? TableId,List<ItemInput> Items,string RequestKey);
public record AddItems(List<ItemInput> Items,string RequestKey);
public record RequestInput(string RequestKey);
public record PayInput(string Method,decimal CashReceived,bool TransferConfirmed,string RequestKey);
public record CancelInput(string Reason);
public record ReviewInput(bool Approve,string Reason);
public record AdjustInput(int IngredientId,decimal Quantity,string Reason,string RequestKey);
public record ProductInput(string Name,decimal Price,string Category,string Icon,bool IsActive,byte[]? Photo = null);
