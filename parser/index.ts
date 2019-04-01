import { CommandLineParser } from "@microsoft/ts-command-line";
import { PingAction } from "../commands/ping";
import { UptimeAction } from "../commands/uptime";
import { ListAction } from "../commands/list";
import { AddAction } from "../commands/add";
import { Message } from "discord.js";

export class DijonCommandParser extends CommandLineParser {
	constructor(private message: Message) {
		super({
			toolFilename: "!dijon",
			toolDescription: "Manages raid team rosters and performs all kinds of tomfoolery."
		});

		this.addAction(new PingAction(message));
		this.addAction(new UptimeAction(message));
		this.addAction(new ListAction(message));
		this.addAction(new AddAction(message));
	}

	protected onDefineParameters(): void {}
}
