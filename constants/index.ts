import { getEnvVar } from "../utils/env";

export const clientId = getEnvVar("DIJON_CLIENT_ID").get();
export const clientSecret = getEnvVar("DIJON_CLIENT_SECRET").get();
export const botUsername = getEnvVar("DIJON_BOT_USERNAME").get();
export const botToken = getEnvVar("DIJON_BOT_TOKEN").get();
export const couchHost = getEnvVar("COUCH_HOST").get();
