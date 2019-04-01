import { CommandLineAction } from "@microsoft/ts-command-line";
import { Message, RichEmbed } from "discord.js";
import { formatDateTime } from "../utils/format-datetime";

export class UptimeAction extends CommandLineAction {
	constructor(private message: Message) {
		super({
			actionName: "uptime",
			documentation: "Returns the date the bot came online.",
			summary: "Returns the date the bot came online."
		});
	}

	protected onDefineParameters(): void {}

	protected async onExecute(): Promise<void> {
		const since = formatDateTime(Date.now() - this.message.client.uptime);
		const embed = new RichEmbed()
			.setColor("GREEN")
			.setTitle(":stopwatch: Uptime")
			.setDescription(`${this.message.client.user.username} has been online since **${since}**.`);

		await this.message.channel.send(embed);
	}
}
