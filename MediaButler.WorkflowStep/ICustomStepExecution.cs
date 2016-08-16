namespace MediaButler.WorkflowStep
{
    public interface ICustomStepExecution
    {
        /// <summary>
        /// Implement Custome logic Step
        /// </summary>
        /// <param name="request">Custom request</param>
        /// <returns>true</returns>
        bool execute(ICustomRequest request);
    }
}
