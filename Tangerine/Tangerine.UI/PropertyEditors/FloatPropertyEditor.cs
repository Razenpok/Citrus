using Lime;
using Tangerine.Core;
using Tangerine.Core.ExpressionParser;

namespace Tangerine.UI
{
	public class FloatPropertyEditor : CommonPropertyEditor<float>
	{
		private NumericEditBox editor;

		public FloatPropertyEditor(IPropertyEditorParams editorParams) : base(editorParams)
		{
			editor = editorParams.NumericEditBoxFactory();
			EditorContainer.AddNode(editor);
			EditorContainer.AddNode(Spacer.HStretch());
			var current = CoalescedPropertyValue();
			editor.Submitted += text => SetComponent(text, current.GetValue());
			editor.AddChangeWatcher(current, v => editor.Text = v.IsUndefined ? v.Value.ToString("0.###") : ManyValuesText);
		}

		public void SetComponent(string text, CoalescedValue<float> current)
		{
			if (Parser.TryParse(text, out double newValue) &&
			    PropertyValidator.ValidateValue((float)newValue, EditorParams.PropertyInfo)) {
				SetProperty((float)newValue);
			}
			editor.Text = current.IsUndefined ? current.Value.ToString("0.###") : ManyValuesText;
		}

		public override void Submit()
		{
			var current = CoalescedPropertyValue();
			SetComponent(editor.Text, current.GetValue());
		}
	}
}
