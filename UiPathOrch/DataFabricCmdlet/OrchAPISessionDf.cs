using System.Net.Http;
using UiPath.PowerShell.Entities;

namespace UiPath.OrchAPI;

// UiPath Data Fabric (DataService) API — rooted at /dataservice_/api.
// Requires DataFabric.Schema.Read (entity list) and DataFabric.Data.Read / .Write (records).
//
// Folder scoping: entities created in a modern folder are only returned when the
// X-UIPATH-OrganizationUnitId header is set to the numeric Orchestrator folder Id.
// Legacy (pre-folder) entities are returned when the header is omitted.
public partial class OrchAPISession
{
    // List Data Fabric entity schemas scoped to the given folder (null = tenant/root).
    public DfEntity[]? GetDfEntities(Int64? folderId = null)
    {
        return HttpRequest<DfEntity[]>(HttpMethod.Get, "/dataservice_/api/Entity", folderId);
    }

    public string GetDfRecord(string entityName, string id, Int64? folderId = null)
    {
        return HttpRequest(HttpMethod.Get, $"/dataservice_/api/EntityService/{entityName}/read/{id}", folderId);
    }

    // POST the filterGroup-shaped query body. The raw JSON body is returned to the caller
    // so it can be rehydrated into dynamic PSObjects without tying the field types to a
    // compile-time schema.
    public string QueryDfEntity(string entityName, object queryBody, Int64? folderId = null)
    {
        return HttpRequest(HttpMethod.Post, $"/dataservice_/api/EntityService/{entityName}/query", folderId, queryBody);
    }
}
