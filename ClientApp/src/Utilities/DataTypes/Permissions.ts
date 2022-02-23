export interface IPermissions{
    userType: 'AccountAdmin' | 'ProjectAdmin';
    accountId: string;
    projectIds: string[]
}