using Mass.Spec.Contracts.Workflow;

namespace Mass.Core.Workflows;

public class BurnStep : WorkflowStep
{
    public BurnStep()
    {
        Action = "Burn";
    }
}

public class PatchStep : WorkflowStep
{
    public PatchStep()
    {
        Action = "Patch";
    }
}

public class DeviceStep : WorkflowStep
{
    public DeviceStep()
    {
        Action = "Device";
    }
}

public class PxeStep : WorkflowStep
{
    public PxeStep()
    {
        Action = "Pxe";
    }
}
