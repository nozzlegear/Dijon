import { CommandLineAction, CommandLineStringParameter, CommandLineChoiceParameter } from "@microsoft/ts-command-line";
import { Message, RichEmbed } from "discord.js";
import { RosterDatabase } from "../database";
import { couchHost } from "../constants";
import { Option } from "@nozzlegear/railway";
import * as parser from "../parser";
import { DavenportError } from "davenport";

type AddOptions = {
	name: Option<string>;
	role: Option<Role>;
	help: boolean;
};

export class AddCommand {
	constructor(private name: string) {}

	private allowedRoles: Role[] = ["healer", "melee", "ranged", "tank"];

	execute: (argv: string[], message: Message) => Promise<void> = async (argv, message) => {
		const options: AddOptions = {
			help: parser.parseShowHelp(argv),
			name: parser.findSwitchValue(argv, "--name", "-n"),
			role: parser.findSwitchValue(argv, "--role", "-r")
		};
		const helpMessage = "Add a new user to the Dijon roster with `dijon add --name memberName --role roleName`.";

		if (options.help) {
			await message.channel.send(helpMessage);
			return;
		}

		if (Option.isNone(options.name)) {
			await message.channel.send("You must enter the new member's name. " + helpMessage);
			return;
		}

		if (Option.isNone(options.role)) {
			await message.channel.send("You must enter the new member's role. " + helpMessage);
			return;
		}

		const name = Option.get(options.name);
		const role = Option.get(options.role);

		if (name.length < 3) {
			await message.channel.send(`Name must be at least 3 characters long. ${helpMessage}`);
		}

		if (!this.allowedRoles.includes(role)) {
			await message.channel.send(
				`You must enter a valid role. Allowed values are ${this.allowedRoles.join(", ")}. ${helpMessage}`
			);
			return;
		}

		const teamName = await RosterDatabase.getDefaultTeamName(message.channel.id);
		const db = new RosterDatabase(couchHost, teamName);

		try {
			const result = await db.createMember(name, role);

			await message.channel.send(
				`Created user ${result._id} with role ${result.role}! Use \`!dijon list\` to view all roster members.`
			);
		} catch (_e) {
			const e: DavenportError = _e;
			const error = new RichEmbed().setColor("RED").setTitle(":warning: Database error").setDescription(`${
				e.message
			}
\`\`\`
${JSON.stringify(e.body)}
\`\`\`
				`);

			await message.channel.send(error);
			return;
		}
	};
}

// export class AddAction extends CommandLineAction {
// 	constructor(private message: Message) {
// 		super({
// 			actionName: "add",
// 			documentation: "Adds a user to a team roster if they don't already exist",
// 			summary: "Adds a user to a team roster"
// 		});
// 	}

// 	private allowedRoles: Role[] = ["healer", "melee", "ranged", "tank"];
// 	private teamParameter!: CommandLineStringParameter;
// 	private nameParameter!: CommandLineStringParameter;
// 	private roleParameter!: CommandLineChoiceParameter;

// 	protected onDefineParameters(): void {
// 		this.teamParameter = this.defineStringParameter({
// 			argumentName: "TEAM",
// 			defaultValue: "",
// 			description: "The team that the member will be added to",
// 			parameterLongName: "--team",
// 			parameterShortName: "-t",
// 			required: false
// 		});
// 		this.nameParameter = this.defineStringParameter({
// 			argumentName: "NAME",
// 			description: "The new member's name",
// 			parameterLongName: "--name",
// 			parameterShortName: "-n",
// 			required: true
// 		});
// 		this.roleParameter = this.defineChoiceParameter({
// 			alternatives: this.allowedRoles,
// 			description: "The new member's role",
// 			parameterLongName: "--role",
// 			parameterShortName: "-r",
// 			required: true
// 		});
// 	}

// 	protected async onExecute(): Promise<void> {
// 		const name = this.nameParameter.value;
// 		const role = this.roleParameter.value as Role | undefined;

// 		if (!name || name.length < 3) {
// 			await this.message.channel.send(`Name must be at least 3 characters long, ${this.message.author.id}.`);
// 			return;
// 		}

// 		if (!role || this.allowedRoles.indexOf(role) === -1) {
// 			await this.message.channel.send(`Role must be one of ${this.allowedRoles.join(", ")}`);
// 			return;
// 		}

// 		const teamName = this.teamParameter.value || (await RosterDatabase.getDefaultTeamName(this.message.channel.id));
// 		const db = new RosterDatabase(couchHost, teamName);
// 		const result = await db.createMember(name, role);

// 		await this.message.channel.send(
// 			`Created user ${result._id} with role ${result.role}! Use \`!dijon list\` to view all roster members.`
// 		);
// 	}
// }
