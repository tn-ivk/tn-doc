using TN.DocData;

namespace TN_Doc.Models.Services;

public interface IDbSchemaCache
{
    bool HasDataArm(int deviceId, IdDoc idDoc);
}