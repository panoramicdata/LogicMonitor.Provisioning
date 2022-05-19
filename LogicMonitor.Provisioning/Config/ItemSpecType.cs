namespace LogicMonitor.Provisioning.Config;

public enum ItemSpecType
{
   /// <summary>
   /// The single item is specified entirely in configuration
   /// </summary>
   ConfigSingle,

   /// <summary>
   /// The single item is cloned from this id
   /// </summary>
   CloneSingleFromId,

   /// <summary>
   /// Multiple items are imported from spreadsheet using templating
   /// </summary>
   XlsxMulti
}
