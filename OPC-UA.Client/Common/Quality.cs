using System.ComponentModel;

namespace OPC_UA.Client.Common
{
  /// <summary>
  /// Represents the quality of the value captured
  /// </summary>
  public enum Quality
  {
    /// <summary>
    /// Quality: Unknown, the value of the quality could not be inferred by the library
    /// </summary>
    [Description("Unknown")]
    Unknown,

    /// <summary>
    /// Quality: Good
    /// </summary>
    [Description("Good")]
    Good,

    /// <summary>
    /// Quality: Bad
    /// </summary>
    [Description("Bad")]
    Bad
  }
}