using System.Collections.Generic;

namespace Tests.DocModules.Common;

public class RequestListDocs
{
    public int Id { get; set; }
    public string DT { get; set; }
    public string Description { get; set; }
}

public class CorrectionData
{
    public int DocID { get; set; }
    public List<EditData> Values { get; set; }
}

public class EditData
{
    public string Key { get; set; }
    public string Tag { get; set; }
    public string Value { get; set; }
}