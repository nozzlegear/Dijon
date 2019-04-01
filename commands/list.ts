import { CommandLineAction, CommandLineStringParameter } from "@microsoft/ts-command-line";
import { Message, RichEmbed } from "discord.js";
import { RosterDatabase } from "../database";
import { couchHost } from "../constants";

export class ListAction extends CommandLineAction {
	constructor(private message: Message) {
		super({
			actionName: "list",
			documentation: "Lists all of the members on the team roster.",
			summary: "Lists all of the members on the team roster."
		});
	}

	private teamParameter!: CommandLineStringParameter;

	protected onDefineParameters(): void {
		this.teamParameter = this.defineStringParameter({
			argumentName: "TEAM",
			defaultValue: "",
			description: "The team which should be listed",
			parameterLongName: "--team",
			parameterShortName: "-t",
			required: false
		});
	}

	protected async onExecute(): Promise<void> {
		const teamName = this.teamParameter.value || (await RosterDatabase.getDefaultTeamName(this.message.channel.id));
		const db = new RosterDatabase(couchHost, teamName);
		const roster = await db.createDatabase().then(() => db.listMembers());
		const embed = new RichEmbed()
			.setColor("GREEN")
			.setTitle("Team Roster")
			.setDescription(roster.map(u => `${u.role}: ${u._id}`).join(". "));

		await this.message.channel.send(embed);
	}
}
