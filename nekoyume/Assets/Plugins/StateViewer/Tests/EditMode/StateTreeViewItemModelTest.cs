using Bencodex.Types;
using NUnit.Framework;
using StateViewer.Editor;
using StateViewer.Runtime;

namespace StateViewer.Tests.EditMode
{
    public class StateTreeViewItemModelTest
    {
        [Test, TestCaseSource(typeof(StateViewerWindow), nameof(StateViewerWindow.TestValues))]
        public void Serialize(IValue value)
        {
            // Arrange
            var model = new StateTreeViewItemModel(value);
            // Act
            var serialized = model.Serialize();
            var deserialized = new StateTreeViewItemModel(serialized);
            // Assert
            Assert.AreEqual(value, serialized);
            Assert.AreEqual(model.Value, deserialized.Value);
            Assert.AreEqual(model.Key, deserialized.Key);
        }
    }
}
