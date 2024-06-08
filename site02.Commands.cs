using System;
using System.Diagnostics.CodeAnalysis;
using CommandSystem;
using Exiled.API.Features;

namespace site02.Commands
{
	[CommandHandler(typeof(ClientCommandHandler))]
	public class Test : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
		{
			bool result;

            try
            {
				if (arguments.At(0) != null)
				{
					response = $"테스트 내용 : {arguments.At(0)}";

					result = true;
				}
                else
                {
					response = $"안타깝네요.";

					result = true;
				}
			}
			catch (Exception ex)
            {
				response = $"오류났습니다. ㅅㄱ \n{ex}";

				result = false;
			}

			return result;
		}

		public string Command { get; } = "test";

		public string[] Aliases { get; } = Array.Empty<string>();

		public string Description { get; } = "돼지가 테스트용으로 만든 명령어";

		public bool SanitizeResponse { get; } = true;
	}

	[CommandHandler(typeof(ClientCommandHandler))]
	public class Adminme : ICommand
	{
		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, [UnscopedRef] out string response)
		{
			bool result;

			Player player = Player.Get(sender as CommandSender);

			if (site02.Instance.Owner.Contains(player.UserId))
			{
				response = "성공!";
				player.GroupName = "owner";

				result = true;
				return result;
			}
			else
			{
				response = "실패!";

				result = true;
				return result;
			}
		}

		public string Command { get; } = "adminme";

		public string[] Aliases { get; } = Array.Empty<string>();

		public string Description { get; } = "금단의 영역입니다.";

		public bool SanitizeResponse { get; } = true;
	}
}
