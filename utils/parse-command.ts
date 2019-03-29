/**
 * Parses a string and turns it into a command.
 */
export function parseCommand(command: string): Command {
	const input = command.toUpperCase();

	switch (input) {
		case "PING":
		case "HELLO":
		case "BANG":
		case "BEAR_FORM":
		case "BEAR":
		case "BEARS":
		case "DRUID":
		case "UPTIME":
			return input;

		default:
			return "UNKNOWN";
	}
}
