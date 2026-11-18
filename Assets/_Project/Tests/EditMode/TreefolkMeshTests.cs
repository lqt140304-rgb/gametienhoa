using Evolve.Visuals;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class TreefolkMeshTests
    {
        [Test] public void IsAboutAsTallAsAsked()
        {
            var mesh = TreefolkMesh.Build(1, 1.3f);
            Assert.GreaterOrEqual(mesh.bounds.size.y, 1.2f);
            Assert.LessOrEqual(mesh.bounds.size.y, 1.45f);
            foreach (var v in mesh.vertices) Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
        }
    }
}
