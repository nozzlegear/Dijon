import { Client, DavenportError } from "davenport";
import { RosterMember } from "app/database";

export class RosterDatabase {
	constructor(databaseUrl: string, teamName: string) {
		this.client = new Client(databaseUrl, teamName.toLowerCase());
	}

	private readonly client: Client<RosterMember>;

	/**
	 * Creates the database if it doesn't exist.
	 */
	createDatabase: () => Promise<void> = async () => {
		try {
			const created = await this.client.createDb();
		} catch (_e) {
			const e: DavenportError = _e;

			console.error("Failed to create database: ", e);
		}

		return;
	};

	/**
	 * Lists all members in the database (i.e. on the team).
	 */
	listMembers: () => Promise<RosterMember[]> = async () => {
		const result = await this.client.listWithDocs();

		return result.rows;
	};

	/**
	 * Adds a member to the team.
	 */
	createMember: (username: string, role: Role) => Promise<RosterMember> = async (username, role) => {
		const date = Date.now();
		const result = await this.client.post({ _id: username, date_joined: date, role: role });

		return {
			date_joined: date,
			role: role,
			_id: result.id,
			_rev: result.rev
		};
	};
}
