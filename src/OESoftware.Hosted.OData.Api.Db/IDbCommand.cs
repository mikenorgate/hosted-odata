using System.Threading.Tasks;

namespace OESoftware.Hosted.OData.Api.Db
{
    public interface IDbCommand
    {
        Task Execute(string tenantId);
    }
}
