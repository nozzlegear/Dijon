// This module can only be called a parser in the barest sense of the word. It just looks for "--help", "--version", and "-f fileName" in a string array.
import { Option } from "@nozzlegear/railway";
import { findIndexIgnoreCase } from "../utils/array";

/**
 * Finds the switch value of @param alias in @param argv while ensuring the value isn't a switch itself (e.g. "--file --database" is passed).
 */
export function findSwitchValue<T extends string = string>(argv: string[], ...aliases: string[]): Option<T> {
	const switchIndex = findIndexIgnoreCase(argv, ...aliases);

	if (switchIndex === -1) {
		return Option.ofNone();
	}

	const value = argv[switchIndex + 1];

	// The value cannot start with a - or --, as that indicates that it's a switch instead of a true value.
	// TODO: How would you handle values that legitimately start with a dash though?
	return !value ? Option.ofNone() : value.startsWith("-") ? Option.ofNone() : Option.ofSome<T>(value as T);
}

/**
 * Parses command line args in search of "--help". Returns true if found.
 */
export const parseShowHelp = (argv: string[]) => findIndexIgnoreCase(argv, "-h", "--help") > -1;

/**
 * Parses command line args in search of "--version". Returns true if found.
 */
export const parseShowVersion = (argv: string[]) => findIndexIgnoreCase(argv, "-v", "--version") > -1;
