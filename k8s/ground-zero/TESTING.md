이 문서에서는 정기 테스트 당번이 정기 테스트를 진행하기 위해 준비해야 하는 것들에 대해서 다룹니다.

# 준비하기

## AWS CLI

1. Swen 에게 요청해서 AWS IAM 계정을 생성합니다.
2. [공식 문서](https://docs.aws.amazon.com/ko_kr/cli/latest/userguide/cli-chap-install.html)를 참조하여 AWS CLI를 설치합니다.

## 클러스터 생성하기

1. [eksctl](https://github.com/weaveworks/eksctl)을 설치합니다.
2. eksctl 을 이용하여 클러스터를 생성합니다.

        $ eksctl create cluster -n <cluster-name> -r <region-name> --nodegroup-name <nodegroup-name> -N <number-of-nodes>

3. 다음 명령으로 `kubectl`에서 사용할 클러스터를 생성한 클러스터로 변경합니다.

        $ aws eks update-kubeconfig --name <cluster-name>

4. 다음 명령으로 configmap 설정을 띄웁니다.

        $ kubectl edit configmap -n kube-system aws-auth

5. mapRoles 아래에 아래 그룹을 추가합니다.

        - groups:
          - system:masters
          rolearn: arn:aws:iam::319679068466:role/EKS
          username: admins

6. 다음 명령을 입력하여 configmap 설정이 반영 되었는지 확인합니다.

        $ aws eks update-kubeconfig --name <cluster-name> --role-arn arn:aws:iam::319679068466:role/EKS
        $ kubectl get pod


# 시작 버전 정하기

- 테스트 기간에 사용할 9C 빌드 버전을 정하기 위해서, 에디터에서 검수된 태그 (예. [1](https://github.com/planetarium/nekoyume-unity/tree/20190910-01), [2](https://github.com/planetarium/nekoyume-unity/tree/20190906-01))를 확인합니다.
- Windows x64, macOS, Linux (헤들리스)에서 모두 정상 동작하는 버전 중, 가장 최신 태그를 기준으로 결정합니다.
- [9C 업스트림](https://github.com/planetarium/nekoyume-unity)의 `master`브랜치에서 관리 되고 있는 9C 코드를 쓰는 것을 기본으로 하되, 부득이 이를 사용할 수 없는 경우 별도 브랜치를 따로 따서 팀원이 접근할 수 있는 저장소에 푸시한 후, 테스트 진행 문서에 추가합니다.

# 시드 노드 빌드하기

네트워크를 운영하기 위해서는 시드(Seed) 노드가 필요합니다. 정기 테스트에서는 이 시드 노드를 Docker 이미지로 빌드하여 Kubernetes(k8s) 를 통해 실행합니다.

시드 노드 이미지를 만들기 위해서는 CircleCI를 사용하거나 프로그래머의 개발 환경에서 직접 빌드할 수 있습니다.

## CircleCI 사용

9C 업스트림에 풀 리퀘스트를 올리면 자동으로 CircleCI가 붙어서 Docker 이미지를 빌드하고 지정한 레지스트리에 푸시합니다. 이 이미지 이름은 CircleCI 빌드 결과 페이지에서 확인할 수 있습니다.

[](https://www.notion.so/7410267a49e947799e15169f8955e87d#5c6914008a594a72a43e5dbb4241684c)

예를 들어 [https://app.circleci.com/jobs/github/planetarium/nekoyume-unity/5073](https://app.circleci.com/jobs/github/planetarium/nekoyume-unity/5073) 배포는 [`319679068466.dkr.ecr.ap-northeast-2.amazonaws.com/nekoyume-unity:git-6ab486c284443f944c9dbd4ff3e12913538ad59a`](http://319679068466.dkr.ecr.ap-northeast-2.amazonaws.com/nekoyume-unity:git-6ab486c284443f944c9dbd4ff3e12913538ad59a) 로 태깅되어 319679068466.dkr.ecr.ap-northeast-2.amazonaws.com 에 푸시됩니다.

## 개발 환경에서 직접 빌드 & 푸시하기

Docker가 설치된 환경이라면 다음 명령어로 이미지를 빌드할 수 있습니다.

    docker build -t 319679068466.dkr.ecr.ap-northeast-2.amazonaws.com/nekoyume-unity:<적절한 TAG> --build-arg ulf="<Base64로 인코딩 된 ULF>" .

- ULF(Unity License File)은 다음과 같은 방법으로 만들 수 있습니다.
    - [이 링크](https://docs.unity3d.com/kr/2019.1/Manual/ManualActivationGuide.html)를 참고하여 직접 만든 다음 base64로 인코딩 하거나
        - 주의) Docker 컨테이너 안에 있는 Unity 에디터에서 요청을 생성해야 합니다.
    - 1Password Vault에서 `Swen's ULF (base64 encoded)`항목을 복붙

만들어진 이미지를 저장소에 푸시하기 위해서는 우선 [Amazon ECR](https://aws.amazon.com/ko/ecr/) 인증을 사용하고 있는 Docker 클라이언트에 통합해야 합니다.

    $ aws ecr get-login --region ap-northeast-2 --no-include-email | sh

그 다음 Docker 이미지를 지정한 레지스트리에 푸시합니다.

    $ docker push 319679068466.dkr.ecr.ap-northeast-2.amazonaws.com/nekoyume-unity:<빌드에 사용한 TAG>


# 블록체인 초기화 방법

- 기존에 있던 마이너, 시드를 내립니다.


    $ kubectl.exe scale --replicas=0 sts/miner
    $ kubectl.exe scale --replicas=0 sts/seed


- pvc를 삭제합니다.


    $ kubectl.exe get pvc -o yaml | kubectl.exe delete -f -


- 유니티 에디터에서 `Tools/Libplanet/Mine Genesis Block` 을 실행해서 새로운 제네시스 블록을 생성합니다.
- s3의 `9c-test/genesis-block` 파일을 새로 생성한 제네시스 블록 파일로 변경합니다.
- 읽기권한은 퍼블릭으로 설정해야 모든 노드에서 제네시스 블록을 읽어올 수 있습니다.


# k8s 클러스터 설정하기

- [Amazon EKS](https://aws.amazon.com/ko/eks/)에서 돌아가는 [9c-internal 클러스터](https://ap-northeast-2.console.aws.amazon.com/eks/home?region=ap-northeast-2#/clusters/9c-internal)를 사용하고 있습니다. 이 클러스터를 사용하기 위해서 아래와 같은 단계로 인증합니다.

        $ aws configure
        AWS Access Key ID [None]: <발급 받은 IAM 계정 엑세스 키>
        AWS Secret Access Key [None]: <발급 받은 IAM 계정 비밀 엑세스 키>
        Default region name [None]: ap-northeast-2
        Default output format [None]: json

        $ aws eks update-kubeconfig --name 9c-internal --role-arn arn:aws:iam::319679068466:role/EKS

- [다음 페이지](https://codepen.io/hongminhee/pen/LBJPQp)에서 시드 노드에서 사용할 비밀키 / 공개 키를 생성합니다.
- `k8s/ground-zero/deployment.yaml.template` 을 적절히 수정하여 `k8s/ground-zero/deployment.yaml`을 만듭니다.
- 다음 명령으로 변경된 템플릿을 클러스터에 반영합니다.

        $ kubectl apply -f k8s/ground-zero/deployment.yaml

# 클라이언트 빌드 하기

- `nekoyume/Assets/StreamingAssets/clo.json` 파일을 만들어서 아래와 같이 편집합니다.

        {
            "noMiner": true,
            "peers": ["<시드 노드의 공개키>,ground-zero.planetarium.dev,31234"]
        }

    - `clo.json`파일의 자세한 설정은 nekoyume-unity 프로젝트의 [README](https://github.com/planetarium/nekoyume-unity/blob/266b6ee/README.md)를 참고하세요.

- Unity (2019.1) 에디터의 상단 메뉴의 Build → Windows / Mac OSX / Linux 빌드를 눌러 프로젝트를 빌드합니다.
- `nekoyume/Build`에 플랫폼 별로 생성된 빌드 폴더를 zip으로 압축해서 공유합니다.
    - Windows에서 7z으로 압축시 macOS에서 실행 권한이 없다고 나오는 경우가 있습니다.
