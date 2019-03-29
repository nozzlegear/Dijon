import { Option } from "@nozzlegear/railway";

/**
 * Attempts to get an environment variable from the environment.
 */
export const getEnvVar: (name: string) => Option<string> = name => {
	const value = process.env[name];

	return value ? Option.ofSome(value) : Option.ofNone();
};
