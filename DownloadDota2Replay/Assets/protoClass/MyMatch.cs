//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#pragma warning disable 1591

// Generated from: my_dota_gcmessages_common.proto
// Note: requires additional types generated from: dota_gcmessages_common.proto
namespace DownloadDota2Replay
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"MyMatch")]
  public partial class MyMatch : global::ProtoBuf.IExtensible
  {
    public MyMatch() {}
    

    private CMsgDOTAMatch _matchDetail = null;
    [global::ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"matchDetail", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(null)]
    public CMsgDOTAMatch matchDetail
    {
      get { return _matchDetail; }
      set { _matchDetail = value; }
    }

    private bool _isDownloaded = default(bool);
    [global::ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"isDownloaded", DataFormat = global::ProtoBuf.DataFormat.Default)]
    [global::System.ComponentModel.DefaultValue(default(bool))]
    public bool isDownloaded
    {
      get { return _isDownloaded; }
      set { _isDownloaded = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
#pragma warning restore 1591