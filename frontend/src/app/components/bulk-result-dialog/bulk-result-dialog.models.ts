import {AdminCreateUserDto} from '../../api';

export interface BulkResultItem {
  key: string;
  input: AdminCreateUserDto;
  success: boolean;
  errorCode?: string | null;
  errorMessage?: string | null;
}
