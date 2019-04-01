import { CommandLineAction, CommandLineStringParameter, CommandLineChoiceParameter } from "@microsoft/ts-command-line";
import { Message } from "discord.js";
import { RosterDatabase } from "../database";
import { couchHost } from "../constants";

export class AddAction extends CommandLineAction {
	constructor(private message: Message) {
		super({
			actionName: "add",
			documentation: "Adds a user to a team roster if they don't already exist",
			summary: "Adds a user to a team roster"
		});
	}

	private allowedRoles: Role[] = ["healer", "melee", "ranged", "tank"];
	private teamParameter!: CommandLineStringParameter;
	private nameParameter!: CommandLineStringParameter;
	private roleParameter!: CommandLineChoiceParameter;

	protected onDefineParameters(): void {
		this.teamParameter = this.defineStringParameter({
			argumentName: "TEAM",
			defaultValue: "",
			description: "The team that the member will be added to",
			parameterLongName: "--team",
			parameterShortName: "-t",
			required: false
		});
		this.nameParameter = this.defineStringParameter({
			argumentName: "NAME",
			description: "The new member's name",
			parameterLongName: "--name",
			parameterShortName: "-n",
			required: true
		});
		this.roleParameter = this.defineChoiceParameter({
			alternatives: this.allowedRoles,
			description: "The new member's role",
			parameterLongName: "--role",
			parameterShortName: "-r",
			required: true
		});
	}

	protected async onExecute(): Promise<void> {
		const name = this.nameParameter.value;
		const role = this.roleParameter.value as Role | undefined;

		if (!name || name.length < 3) {
			await this.message.channel.send(`Name must be at least 3 characters long, ${this.message.author.id}.`);
			return;
		}

		if (!role || this.allowedRoles.indexOf(role) === -1) {
			await this.message.channel.send(`Role must be one of ${this.allowedRoles.join(", ")}`);
			return;
		}

		const teamName = this.teamParameter.value || (await RosterDatabase.getDefaultTeamName(this.message.channel.id));
		const db = new RosterDatabase(couchHost, teamName);
		const result = await db.createMember(name, role);

		await this.message.channel.send(
			`Created user ${result._id} with role ${result.role}! Use \`!dijon list\` to view all roster members.`
		);
	}
}
