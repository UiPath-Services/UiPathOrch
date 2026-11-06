using System.Management.Automation;

namespace UiPath.PowerShell.Commands;

// Type names added in front of an object's own, only so that UiPathOrch.Format.ps1xml can give
// one cmdlet's output a view of its own. The object keeps its .NET type (it pipes and binds as
// before); only PowerShell's formatting sees the added name. Used instead of an empty subclass
// whenever the difference is the view alone.
internal static class ViewTypeNames
{
    // Get-OrchTaskAcrossFolder: one table across the folders, the folder as a column.
    public const string TaskAcrossFolder = "UiPath.PowerShell.Entities.OrchTask#AcrossFolder";

    // New-OrchMachine: the machine just created, with the key it was given.
    public const string CreatedMachine = "UiPath.PowerShell.Entities.ExtendedMachine#Created";

    // New- / Copy-PmExternalApplication: the application just created, with its client id and,
    // for a confidential one, the secret, which is shown only now.
    public const string CreatedExternalClient = "UiPath.PowerShell.Entities.ExternalClient#Created";

    public static PSObject WithViewTypeName(object obj, string typeName)
    {
        var pso = PSObject.AsPSObject(obj);
        if (!pso.TypeNames.Contains(typeName)) pso.TypeNames.Insert(0, typeName);
        return pso;
    }
}
