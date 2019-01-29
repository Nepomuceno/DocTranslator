using System;

public class DocDetails
{
    public long Size { get; set; }
    public string ContentMD5 { get; set; }
    public Uri Uri { get; set; }
    public string Name { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    
}