namespace Mass.Core.Workflows;

public class BurnStep : WorkflowStep
{
    public BurnStep()
    {
        Type = "Burn";
    }
}

public class PatchStep : WorkflowStep
{
    public PatchStep()
    {
        Type = "Patch";
    }
}

public class DeviceStep : WorkflowStep
{
    public DeviceStep()
    {
        Type = "Device";
    }
}

public class PxeStep : WorkflowStep
{
    public PxeStep()
    {
        Type = "Pxe";
    }
}
