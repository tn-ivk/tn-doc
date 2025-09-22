using TN.DocData;

namespace TN_Doc.Services;

public interface IDbSchemaCache
{
    bool HasDataArm(int deviceId, IdDoc idDoc);
}