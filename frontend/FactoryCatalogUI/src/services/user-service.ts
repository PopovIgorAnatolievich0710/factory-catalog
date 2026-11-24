import { catalogApi } from "../helpers/api-helper";
import { USER_ME_API_URL } from "../constants/urls";
import { CurrentUser } from "../entities/user";

class UserService {
    fetchMe(): Promise<CurrentUser> {
        return catalogApi.get<CurrentUser>(USER_ME_API_URL);
    }
}

const userService = new UserService();
export default userService;
