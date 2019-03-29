import { Client, RichEmbed } from "discord.js";
import { formatDateTime } from "./utils/format-datetime";
import { getEnvVar } from "./utils/env";
import { parseCommand } from "./utils/parse-command";

const clientId = getEnvVar("DIJON_CLIENT_ID").get();
const clientSecret = getEnvVar("DIJON_CLIENT_SECRET").get();
const botUsername = getEnvVar("DIJON_BOT_USERNAME").get();
const botToken = getEnvVar("DIJON_BOT_TOKEN").get();
const client = new Client();

client.on("ready", () => {
	console.log("Dijon-bot is ready!");
});

client.on("message", async msg => {
	const botRegex = /^!dijon +/i;
	const commandRegex = /^\w+/i;

	if (!botRegex.test(msg.content)) {
		return;
	}

	const withoutName = msg.content.replace(botRegex, "").trim();
	const message = withoutName.replace(commandRegex, "").trim();
	const [command] = commandRegex.exec(withoutName) || ["UNKNOWN"];

	switch (parseCommand(command)) {
		case "HELLO":
			msg.channel.send("Hello world!");
			break;

		case "PING": {
			const embed = new RichEmbed()
				.setColor("GREEN")
				.setTitle(":ping_pong: Pong!")
				.setDescription(`:heartbeat: **${client.ping} ms** heartbeat latency.`);

			await msg.channel.send(embed);

			break;
		}

		case "UPTIME": {
			const since = formatDateTime(Date.now() - client.uptime);
			const embed = new RichEmbed()
				.setColor("GREEN")
				.setTitle(":stopwatch: Uptime")
				.setDescription(`${client.user.username} has been online since **${since}**.`);

			await msg.channel.send(embed);
			break;
		}

		case "BANG":
			msg.channel.send("🍆").then(() => {
				msg.react("💦");
			});
			break;

		case "BEAR_FORM":
		case "BEAR":
		case "BEARS":
		case "DRUID": {
			const embed = new RichEmbed()
				.setColor("ORANGE")
				.setImage("https://az.nozzlegear.com/images/share/2019-03-29.15.29.55.png")
				.setTitle("Bears are for fite! 🐻");

			await msg.channel.send(embed);

			break;
		}

		case "UNKNOWN":
			msg.channel.send(`Dijon-bot does not recognize the ${command} command.`);
			break;
	}
});

process.on("SIGINT", () => {
	console.log("Terminating...");
	client.destroy().then(() => process.exit(0));
});

process.on("SIGTERM", () => {
	client.destroy().then(() => process.exit(0));
});

client.login(botToken);
