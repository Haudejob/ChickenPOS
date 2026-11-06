using System.Collections.Concurrent;
namespace ChickenPos.Web;
// Single-server demo session revocation. A distributed deployment needs a shared session store.
public sealed class SessionRegistry
{
    readonly ConcurrentDictionary<string,(int UserId,DateTimeOffset Until)> sessions=new();
    public string Create(int userId){
        foreach(var pair in sessions)if(pair.Value.Until<=DateTimeOffset.UtcNow)sessions.TryRemove(pair.Key,out _);
        var id=Guid.NewGuid().ToString("N");sessions[id]=(userId,DateTimeOffset.UtcNow.AddHours(4));return id;
    }
    public bool Valid(string? id,int userId)=>id is not null&&sessions.TryGetValue(id,out var s)&&s.UserId==userId&&s.Until>DateTimeOffset.UtcNow;
    public void Revoke(string? id){if(id is not null)sessions.TryRemove(id,out _);}
}
