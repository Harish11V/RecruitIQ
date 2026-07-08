export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  tenantId: string;
}
