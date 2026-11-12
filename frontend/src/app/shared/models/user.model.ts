/** Mirrors the API's UserDto (never contains the password hash). */
export interface User {
  id: string;
  email: string;
  name: string;
}
