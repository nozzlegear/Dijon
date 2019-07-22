import { Message, RichEmbed } from "discord.js";
import { CommandLineAction } from "@microsoft/ts-command-line";

export class PingAction extends CommandLineAction {
	constructor(private message: Message) {
		super({
			actionName: "ping",
			documentation: "Returns the bot's ping.",
			summary: "Returns the bot's ping."
		});
	}

	protected onDefineParameters(): void {}

	protected async onExecute(): Promise<void> {
		const embed = new RichEmbed()
			.setColor("GREEN")
			.setTitle(":ping_pong: Pong!")
			.setDescription(`:heartbeat: **${this.message.client.ping} ms** heartbeat latency.`);

		await this.message.channel.send(embed);
	}
}
