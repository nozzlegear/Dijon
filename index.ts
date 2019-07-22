import { Client } from "discord.js";
import * as parser from "./parser";
import { botToken } from "./constants";
import { parseCommand } from "./utils/parse-command";
import { Option } from "@nozzlegear/railway";
import { AddCommand } from "./commands/add";

const client = new Client();

client.on("ready", () => {
	console.log("Dijon-bot is ready!");
});

client.on("message", async msg => {
	const botRegex = /^!dijon +/i;
	const commandRegex = /^\w+/i;
	const wordRegex = /^\w+/i;

	if (!botRegex.test(msg.content)) {
		return;
	}

	const withoutName = msg.content.replace(botRegex, "").trim();
	const args = withoutName.split(" ");
	const [command] = commandRegex.exec(withoutName) || ["UNKNOWN"];

	switch (parseCommand(command)) {
		case "ADD":
			const addCommand = new AddCommand("ADD");
			await addCommand.execute(args, msg);
			break;

		default:
			await msg.channel.send("Command not found.");
			break;
	}

	// const parser = new DijonCommandParser(msg);
	// const result = await parser.execute(args);

	// const message = withoutName.replace(commandRegex, "").trim();
	// const db = new RosterDatabase(couchHost, "DEFAULT");

	// switch (parseCommand(command)) {
	// 	case "HELLO":
	// 		msg.channel.send("Hello world!");
	// 		break;

	// 	case "PING": {
	// 		const embed = new RichEmbed()
	// 			.setColor("GREEN")
	// 			.setTitle(":ping_pong: Pong!")
	// 			.setDescription(`:heartbeat: **${client.ping} ms** heartbeat latency.`);

	// 		await msg.channel.send(embed);

	// 		break;
	// 	}

	// 	case "UPTIME": {
	// 		const since = formatDateTime(Date.now() - client.uptime);
	// 		const embed = new RichEmbed()
	// 			.setColor("GREEN")
	// 			.setTitle(":stopwatch: Uptime")
	// 			.setDescription(`${client.user.username} has been online since **${since}**.`);

	// 		await msg.channel.send(embed);
	// 		break;
	// 	}

	// 	case "LIST": {
	// 		const roster = await db.createDatabase().then(() => db.listMembers());
	// 		const embed = new RichEmbed()
	// 			.setColor("GREEN")
	// 			.setTitle("Team Roster")
	// 			.setDescription(roster.map(u => `${u.role}: ${u._id}`).join(". "));

	// 		await msg.channel.send(embed);

	// 		break;
	// 	}

	// 	case "ADD": {
	// 		const [username] = wordRegex.exec(message) || [null];

	// 		if (!username) {
	// 			await msg.channel.send("Error: you must enter a username. Example: `!dijon add username role`");
	// 			return;
	// 		}

	// 		const newUser = await db.createDatabase().then(() => db.createMember(username, "healer"));

	// 		await msg.channel.send(
	// 			`Created user ${newUser._id} with role ${newUser.role}! Use \`!dijon list\` to view all roster members.`
	// 		);

	// 		break;
	// 	}

	// 	case "BANG":
	// 		msg.channel.send("🍆").then(() => {
	// 			msg.react("💦");
	// 		});
	// 		break;

	// 	case "BEAR_FORM":
	// 	case "BEAR":
	// 	case "BEARS":
	// 	case "DRUID": {
	// 		const embed = new RichEmbed()
	// 			.setColor("ORANGE")
	// 			.setImage("https://az.nozzlegear.com/images/share/2019-03-29.15.29.55.png")
	// 			.setTitle("Bears are for fite! 🐻");

	// 		await msg.channel.send(embed);

	// 		break;
	// 	}

	// 	case "UNKNOWN":
	// 		msg.channel.send(`Dijon-bot does not recognize the ${command} command.`);
	// 		break;
	// }
});

process.on("SIGINT", () => {
	console.log("Terminating...");
	client.destroy().then(() => process.exit(0));
});

process.on("SIGTERM", () => {
	client.destroy().then(() => process.exit(0));
});

client.login(botToken);
