import { CouchDoc } from "davenport";

export interface RosterMember extends CouchDoc {
	role: Role;
	date_joined: number;
}
