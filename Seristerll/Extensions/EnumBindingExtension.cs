using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Seristerll.Extensions
{
	public class EnumBindingExtension : MarkupExtension
	{
		private readonly Type enumType;

		public EnumBindingExtension(Type enumType)
		{
			this.enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));

			if (!enumType.IsEnum)
			{
				throw new ArgumentException($"{enumType} is not enum.");
			}
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return Enum.GetValues(this.enumType);
		}
	}
}
