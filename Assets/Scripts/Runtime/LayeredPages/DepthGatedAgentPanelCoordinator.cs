namespace ProjectGaze.Gaze
{
    public enum DepthGatedAgentPanelTransition
    {
        None,
        Show,
        Hide
    }

    public sealed class DepthGatedAgentPanelCoordinator
    {
        public const string AgentLogoPageId = "Agent_Logo";

        public bool IsAgentPanelActive { get; private set; }

        public DepthGatedAgentPanelTransition TickConfirmedPage(string confirmedPageId)
        {
            if (string.IsNullOrEmpty(confirmedPageId))
            {
                return DepthGatedAgentPanelTransition.None;
            }

            if (string.Equals(confirmedPageId, AgentLogoPageId, System.StringComparison.Ordinal))
            {
                if (IsAgentPanelActive)
                {
                    return DepthGatedAgentPanelTransition.None;
                }

                IsAgentPanelActive = true;
                return DepthGatedAgentPanelTransition.Show;
            }

            if (!IsAgentPanelActive)
            {
                return DepthGatedAgentPanelTransition.None;
            }

            IsAgentPanelActive = false;
            return DepthGatedAgentPanelTransition.Hide;
        }
    }
}
