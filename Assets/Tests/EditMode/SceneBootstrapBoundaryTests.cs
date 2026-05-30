using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectGaze.Gaze;
using UnityEngine;

namespace ProjectGaze.Tests
{
    public sealed class SceneBootstrapBoundaryTests
    {
        [Test]
        public void DepthGatedAgentBootstrap_OwnsRuntimeInitializeEntryPoint()
        {
            Assert.That(HasRuntimeInitializeMethod(typeof(DepthGatedAgentBootstrap)), Is.True);
            Assert.That(HasRuntimeInitializeMethod(typeof(DepthGatedAgentDemo)), Is.False);
        }

        private static bool HasRuntimeInitializeMethod(System.Type type)
        {
            return type
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(method => method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), inherit: false).Length > 0);
        }
    }
}
