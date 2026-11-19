using System.Linq;
using Evolve.World;
using NUnit.Framework;

namespace Evolve.Tests.EditMode
{
    public class FloraDownloadTests
    {
        // the shape of https://api.polyhaven.com/files/{id} (trimmed)
        const string Files = "{\"fbx\":{\"1k\":{\"fbx\":{\"url\":\"https://dl/x/fern_02_1k.fbx\",\"size\":1,\"include\":{\"textures/fern_02_diff_1k.jpg\":{\"url\":\"https://dl/t/d1.jpg\",\"size\":1}}}}," +
                             "\"2k\":{\"fbx\":{\"url\":\"https://dl/x/fern_02_2k.fbx\",\"size\":1,\"include\":{" +
                             "\"textures/fern_02_diff_2k.jpg\":{\"url\":\"https://dl/t/d.jpg\",\"size\":1},\"textures/fern_02_nor_gl_2k.exr\":{\"url\":\"https://dl/t/n.exr\",\"size\":1}," +
                             "\"textures/fern_02_rough_2k.exr\":{\"url\":\"https://dl/t/r.exr\",\"size\":1},\"textures/fern_02_alpha_2k.png\":{\"url\":\"https://dl/t/a.png\",\"size\":1}}}}}," +
                             "\"gltf\":{\"2k\":{\"gltf\":{\"url\":\"https://dl/x/fern_02_2k.gltf\",\"size\":1}}}}";
        static FloraAsset Fern(string res) => new FloraAsset { id = "fern_02", resolution = res };

        [Test]
        public void PickUrl_Model_PrefersFbxThenGltf()
        {
            Assert.AreEqual("https://dl/x/fern_02_2k.fbx", FloraDownload.PickUrl(Files, Fern("2k"), out var name));
            Assert.AreEqual("fern_02_2k.fbx", name);
            Assert.AreEqual("https://dl/x/fern_02_1k.fbx", FloraDownload.PickUrl(Files, Fern("1k"), out _));
            const string onlyGltf = "{\"gltf\":{\"2k\":{\"gltf\":{\"url\":\"https://dl/x/a.gltf\"}}}}";
            Assert.AreEqual("https://dl/x/a.gltf", FloraDownload.PickUrl(onlyGltf, Fern("2k"), out _));
        }

        [Test]
        public void PickUrl_Texture_ReturnsDiffNorRoughAo()
        {
            var plan = FloraDownload.Plan(Files, Fern("2k"), null);
            Assert.AreEqual(5, plan.Count, "the model and its 4 textures");
            Assert.IsTrue(plan.Any(p => p.path == "Assets/_Project/ThirdParty/Scanned/fern_02/textures/fern_02_nor_gl_2k.exr"));
            Assert.IsTrue(plan.Any(p => p.path == "Assets/_Project/ThirdParty/Scanned/fern_02/fern_02_2k.fbx"));
        }

        [Test]
        public void PickUrl_MissingFormat_ReturnsNull()
        {
            Assert.IsNull(FloraDownload.PickUrl(Files, Fern("8k"), out _));
            Assert.IsNull(FloraDownload.PickUrl("{}", Fern("2k"), out _));
            Assert.IsNull(FloraDownload.PickUrl("broken{", Fern("2k"), out _));
            Assert.AreEqual(0, FloraDownload.Plan("broken{", Fern("2k"), null).Count);
        }

        [Test]
        public void Plan_SkipsExistingFiles()
        {
            var plan = FloraDownload.Plan(Files, Fern("2k"), p => p.EndsWith("_diff_2k.jpg"));
            Assert.AreEqual(4, plan.Count);
        }
    }
}
